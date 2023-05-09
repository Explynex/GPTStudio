﻿using GPTStudio.OpenAI.Chat;
using GPTStudio.OpenAI.Models;
using GPTStudio.TelegramProvider.Commands;
using GPTStudio.TelegramProvider.Database;
using GPTStudio.TelegramProvider.Database.Models;
using GPTStudio.TelegramProvider.Globalization;
using GPTStudio.TelegramProvider.Utils;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Env = GPTStudio.TelegramProvider.Infrastructure.Configuration;
namespace GPTStudio.TelegramProvider;


internal class App
{
    public static bool IsShuttingDown                  = false;
    public static readonly HashSet<long> NowGeneration = new();

    static void Main(string[] args)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("   ___   ___   _____   ___   _               _   _       \n" +
            "  / __| | _ \\ |_   _| / __| | |_   _  _   __| | (_)  ___ \n" + " | (_ | |  _/   | |   \\__ \\ |  _| | || | / _` | | | / _ \\\n"
            + "  \\___| |_|     |_|   |___/  \\__|  \\_,_| \\__,_| |_| \\___/\n_________________________________________________________________\n");
        Console.ForegroundColor = ConsoleColor.White;

        Env.Setup();
        Connection.Connect();
        Env.Client.StartReceiving(OnUpdateHandler, OnErrorHandler);
        Logger.Print($"Telegram update callback processing...");

        while(!IsShuttingDown)
        {
            Logger.Print("Command: /",false);
            var cmd = Console.ReadLine();
            if (!string.IsNullOrEmpty(cmd))
                ConsoleHandler.HandleConsoleCommand(cmd);
        }
    }

    private static async Task OnUpdateHandler(ITelegramBotClient sender, Update e,CancellationToken cancellationToken)
    {
        var senderUser = e.Message?.From == null ? e.CallbackQuery?.From : e.Message.From;
        var chatId     = e.Message?.From == null ? e.CallbackQuery?.Message?.Chat.Id : e.Message.Chat.Id;

        if (senderUser == null)
            return;

        if (!Connection.Users.FindFirst(o => o.Id == senderUser.Id, out GUser user))
        {
            var isSupportedLang = Globalization.Locale.SupportedLocales.Contains(senderUser.LanguageCode);

            Logger.Print("OnUpdateHandler() | Joined new user: @" + senderUser.Username + " , ID: " + senderUser.Id);
            Connection.Users.InsertOne(user = new GUser(senderUser.Id) { LocaleCode = isSupportedLang ? senderUser.LanguageCode : null});

            if (chatId != null)
                await Env.Client.SendTextMessageAsync(chatId, $"{Locale.Cultures[user.LocaleCode!][Strings.FirstHelloMsg]} {senderUser.FirstName} ?");
        }
        else if(senderUser.Username != user.Username)
        {
            Connection.Users.UpdateOne(o => o.Id == senderUser.Id, Builders<GUser>.Update.Set(nameof(user.Username), senderUser.Username));
        }

        if (e.Type == UpdateType.CallbackQuery)
        {
            await CommandHandler.HandleCallbackQuery(e.CallbackQuery!, user);
            return;
        }
        
        if (e.Message!.Type == MessageType.Voice)
        {
            e.Message.Text = await Utils.VoiceRecognizer.RecognizeVoice(e.Message.Voice!);
        }
        else if (e.Message.Type != MessageType.Text)
            return;

        if (!Connection.Chats.FindFirst(o => o.Id == e.Message.Chat.Id, out GChat chat))
            Connection.Chats.InsertOne(chat = new(e.Message.Chat.Id)); 

        switch (e.Type)
        {
            case UpdateType.Message:
                if (e.Message.Text?.StartsWith('/') == true)
                    await CommandHandler.HandleCommand(e.Message,user);
                else
                    HandleTextMessage(e.Message, chat,user);
                    
                break;
        }
    }
    
    private static async Task GenerateImage(Telegram.Bot.Types.Message msg,string prompt,int imgCount=1)
    {
        var waitMsg = await Env.Client.SendTextMessageAsync(msg.Chat.Id, "Generating . . .");
        try
        {
            var result = await Env.GPTClient.ImagesEndPoint.GenerateImageAsync(prompt,numberOfResults: imgCount);

            if(result.Count > 1)
            {
                var list = new List<IAlbumInputMedia>();
                foreach (var item in result)
                    list.Add(new InputMediaPhoto(item!));
                await Env.Client.SendMediaGroupAsync(msg.Chat.Id, list,replyToMessageId: msg.MessageId);
            }
            else
                await Env.Client.SendPhotoAsync(msg.Chat.Id, new(result[0]!), replyMarkup: KeyboardBuilder.ImageGenerateMarkup, caption: prompt);

            await Env.Client.DeleteMessageAsync(msg.Chat.Id, waitMsg.MessageId);
            Connection.Users.UpdateOne(new BsonDocument("_id", msg.From.Id), Builders<GUser>.Update.Inc("TotalRequests", 1));
        }
        catch
        {
            await Env.Client.EditMessageTextAsync(msg.Chat.Id, waitMsg.MessageId, "An error occurred while generating");
        }
    }

    private static async void HandleTextMessage(Telegram.Bot.Types.Message msg, GChat chat,GUser user)
    {
        if (NowGeneration.Contains(msg.From!.Id))
            return;

        NowGeneration.Add(msg.From.Id);

        #region Calculation tokens
        var totalTokens = Env.Tokenizer.Calculate(msg.Text);
        if (totalTokens > 4000)
            return;

        var lastMsg = new GChatMessage(msg.MessageId, msg.Text, msg.From.Id) { Tokens = totalTokens, Role = Role.User };

        List<GChatMessage> msgList = new();
        for (int i = chat.Messages.Count - 1, insertIndex = 0; i >= 0; i--)
        {
            var gMsg = chat.Messages[i];


            if (gMsg.MessageType != GMessageType.Text)
                continue;
            if ((totalTokens + gMsg.Tokens) > 4048)
                break;

            totalTokens += gMsg.Tokens;
            msgList.Insert(insertIndex, chat.Messages[i]);
        }

        msgList.Add(lastMsg);
        #endregion

        var button            = InlineKeyboardButton.WithCallbackData(Locale.Cultures[user.LocaleCode][Strings.StopGenerationMsg], $"stop.{msg.From.Id}");
        var request           = new ChatRequest(msgList, Model.GPT3_5_Turbo, maxTokens: 2048);
        var response          = new StringBuilder();
        var sendedMsg         = user.GenFullyMode == true ?
              await Env.Client.SendTextMessageAsync(msg.Chat.Id, Locale.Cultures[user.LocaleCode][Strings.ResponseGenMsg], replyToMessageId: msg.MessageId,replyMarkup: new InlineKeyboardMarkup(button)).ConfigureAwait(false)
            : await Env.Client.SendTextMessageAsync(msg.Chat.Id, ". . .",replyToMessageId: msg.MessageId).ConfigureAwait(false);

        var lastEdit          = ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds();
        var lastEditMsgLength = 0;
        using var cancelToken = new CancellationTokenSource();

        try
        {
            await Env.GPTClient.ChatEndpoint.StreamCompletionAsync(request, result =>
            {
                if (!NowGeneration.Contains(msg.From.Id))
                {
                    cancelToken.Cancel();
                    return;
                }


                if (String.IsNullOrEmpty(result.FirstChoice))
                    return;

                response.Append(result.FirstChoice);

                if (user.GenFullyMode == true)
                    return;

                var offset = ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds();
                if ((offset - lastEdit) >= 1)
                {
                    lastEdit = offset;
                    lastEditMsgLength = response.Length;
                    Env.Client.EditMessageTextAsync(msg.Chat.Id, sendedMsg.MessageId, response.ToString(), replyMarkup: button);
                }

            }, cancelToken.Token).ConfigureAwait(false);

            var responseContent = response.ToString();
            var responseTokens = Env.Tokenizer.Calculate(responseContent);

            if (response.Length != lastEditMsgLength || cancelToken.IsCancellationRequested)
                await Env.Client.EditMessageTextAsync(msg.Chat.Id, sendedMsg.MessageId,
                    cancelToken.IsCancellationRequested ? response.Append(". . .").ToString() : responseContent,
                    parseMode: ParseMode.Markdown).ConfigureAwait(false);

            GChat.PushMessage(msg.Chat.Id, lastMsg);
            GChat.PushMessage(msg.Chat.Id, new(sendedMsg.MessageId, responseContent, null)
            {
                Tokens = responseTokens,
                Role = Role.Assistant
            });

            var userDocument = new BsonDocument("_id", msg.From.Id);
            Connection.Users.UpdateOne(userDocument, Builders<GUser>.Update.Inc("TotalTokensGenerated", responseTokens));
            Connection.Users.UpdateOne(userDocument, Builders<GUser>.Update.Inc("TotalRequests", 1));
        }
        catch
        {
            await Env.Client.EditMessageTextAsync(msg.Chat.Id, sendedMsg.MessageId, Locale.Cultures[user.LocaleCode][Strings.ErrorWhileGenMsg]).ConfigureAwait(false);
        }


        NowGeneration.Remove(msg.From.Id);
    }

    private async static Task OnErrorHandler(ITelegramBotClient sender, Exception e, CancellationToken cancellationToken)
    {
        Logger.PrintError('\n' + e.ToString());
    }
}
