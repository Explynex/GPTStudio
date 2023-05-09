using GPTStudio.TelegramProvider.Database;
using GPTStudio.TelegramProvider.Database.Models;
using GPTStudio.TelegramProvider.Globalization;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using System.Runtime.CompilerServices;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Env = GPTStudio.TelegramProvider.Infrastructure.Configuration;

namespace GPTStudio.TelegramProvider.Commands;
internal static class CommandHandler
{
    public static bool PreviousMsgMenu = false;
    private static async Task OpenMenuContent(Message msg,string subMessage,InlineKeyboardMarkup markup)
    {
        Connection.Chats.FindFirst(new BsonDocument("_id", msg.Chat.Id), out GChat chat);

        if (chat.LastMenuMessageId == msg.MessageId)
        {
            await Env.Client.EditMessageTextAsync(chat.Id, msg.MessageId, subMessage, ParseMode.Html, replyMarkup: markup);
            return;
        }

        if (chat.LastMenuMessageId != null)
        {
            try
            {
                await Env.Client.DeleteMessageAsync(chat.Id, chat.LastMenuMessageId.Value);
            }
            catch { await Env.Client.EditMessageTextAsync(chat.Id,chat.LastMenuMessageId.Value,"Command expired."); }
        }

            
        
        await Env.Client.SendTextMessageAsync(chat.Id, subMessage, ParseMode.Html, replyMarkup: markup);

        Connection.Chats.UpdateOne(new BsonDocument("_id", chat.Id), Builders<GChat>.Update.Set(nameof(GChat.LastMenuMessageId), msg.MessageId + 1));

    }

    private static async Task MainMenuButtonsHandler(CallbackQuery query,char buttonNum, GUser user)
    {
        var locale = Locale.Cultures[user.LocaleCode];
        switch(buttonNum)
        {
            case '1':
                await Env.Client.SendTextMessageAsync(query.Message!.Chat.Id, locale[Strings.StartChattingMsg]);
                break;
            case '2':
                await OpensSettingsMenu(query.Message!, user).ConfigureAwait(false);
                break;
            case '3':
                await OpenMenuContent(query.Message!,
                       $"{locale[Strings.SummaryForMsg]} @{query.From.Username},{query.From.FirstName}\n│\n├{locale[Strings.SummaryMemberSince]}\t{DateTimeOffset.FromUnixTimeSeconds(user.JoinTimestamp)}\n" +
                       $"├{locale[Strings.SummaryTokensGen]}\t{user.TotalTokensGenerated}\n└{locale[Strings.SummaryRequests]}\t{user.TotalRequests}",new(KeyboardBuilder.BackToMainButton(user.LocaleCode)));
                break;
            case '5' when user.IsAdmin == true:
                {
                    using var stream = Utils.Common.StreamFromString(JsonConvert.SerializeObject(Connection.Users.Find(o => o.Id != 0).ToList(),Formatting.Indented));
                    await Env.Client.SendDocumentAsync(query.Message!.Chat.Id, new(stream, $"Users {DateTime.Now:yyyy-MM-dd  HH\\;mm\\;ss}.json"), caption: $"👥 Users database for {DateTime.UtcNow:R}");
                    break;
                }
            case '6' when user.IsAdmin == true:
                {
                    using var stream = Utils.Common.StreamFromString(JsonConvert.SerializeObject(Connection.Chats.Find("{}").ToList(), Formatting.Indented));
                    await Env.Client.SendDocumentAsync(query.Message!.Chat.Id, new(stream, $"Chats {DateTime.Now:yyyy-MM-dd  HH\\;mm\\;ss}.json"),caption: $"📚 Chats database for {DateTime.UtcNow:R}");
                    break;
                }

        }
    }

    private static async Task SettingsMenuButtonsHandler(CallbackQuery query, char buttonNum, GUser user)
    {
        switch (buttonNum)
        {
            case '1':
                user.GenFullyMode = !(user.GenFullyMode ?? false);
                Connection.Users.UpdateOne(new BsonDocument("_id", user.Id), Builders<GUser>.Update.Set(nameof(user.GenFullyMode), user.GenFullyMode));
                await OpensSettingsMenu(query.Message!,user).ConfigureAwait(false);
                break;
            case '3':
                await OpenMenuContent(query.Message!, Locale.Cultures[user.LocaleCode][Strings.LanguagesMenuTitle],
                    KeyboardBuilder.LanguagesMarkup(user.LocaleCode)).ConfigureAwait(false);
                break;
        }
    }


    public static async Task HandleCallbackQuery(CallbackQuery query,GUser user)
    {
        if (query == null) return;

        if (query.Data![0] == '1')
            await MainMenuButtonsHandler(query, query.Data[^1], user).ConfigureAwait(false);
        else if (query.Data[0] == '2')
            await SettingsMenuButtonsHandler(query, query.Data[^1], user).ConfigureAwait(false);
        else if (query.Data == "back1")
            await OpenMainMenu(query.Message!, user).ConfigureAwait(false);
        else if (query.Data == "back2")
            await OpensSettingsMenu(query.Message!, user).ConfigureAwait(false);
        else if (query.Data.StartsWith("stop"))
            App.NowGeneration.Remove(Convert.ToInt64(query.Data.Split('.').Last()));
        else if (query.Data.StartsWith("lang"))
        {
            var lang = query.Data.Split('.');

            if (user.LocaleCode == lang[^1])
                return;

            Connection.Users.UpdateOne(
                new BsonDocument("_id", user.Id), Builders<GUser>.Update.Set(nameof(user.LocaleCode), lang[^1]));
            await Env.Client.SendTextMessageAsync(query.Message!.Chat.Id, $"{Locale.Cultures[lang[^1]][Strings.SuccessChangeLang]}{lang[^2]}").ConfigureAwait(false);
        }
        else if (query.Data.StartsWith("img"))
            return;
        
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static async Task OpenMainMenu(Message msg, GUser user) =>
        await OpenMenuContent(msg, Locale.Cultures[user.LocaleCode][Strings.MainMenuTitle],
                    KeyboardBuilder.MainMenuMarkup(user.LocaleCode,user.IsAdmin)).ConfigureAwait(false);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static async Task OpensSettingsMenu(Message msg, GUser user) =>
        await OpenMenuContent(msg, Locale.Cultures[user.LocaleCode][Strings.SettingsTitle],
                            KeyboardBuilder.SettingsMenuMarkup(user)).ConfigureAwait(false);

    public static async Task HandleCommand(Message command,GUser user)
    {
        switch (command.Text!.Split(' ').First())
        {
            case "/menu" or "/start":
                await OpenMainMenu(command,user).ConfigureAwait(false);
                break;

            case "/image":
                if (command.Text.Length < 5)
                    return;

                GChat.PushMessage(command.Chat.Id, new(command.MessageId, command.Text, user.Id, GMessageType.Command));

                var count = 1;
                int startWith = 7;

                if (int.TryParse(command.Text.AsSpan(8, 1), out int result) && result > 0 && result <= 5)
                {
                    count = result;
                    startWith = 11;
                }


             //   await GenerateImage(command, command.Text.Remove(0, startWith), count);
                break;
        }
    }
}
