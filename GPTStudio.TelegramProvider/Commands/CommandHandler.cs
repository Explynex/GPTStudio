using GPTStudio.TelegramProvider.Database;
using GPTStudio.TelegramProvider.Database.Models;
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

        if (chat.LastMenuMessageId != null && (msg.Date - DateTime.UtcNow) < TimeSpan.FromHours(48))
            await Env.Client.DeleteMessageAsync(chat.Id, chat.LastMenuMessageId.Value);
        await Env.Client.SendTextMessageAsync(chat.Id, subMessage, ParseMode.Html, replyMarkup: markup);

        Connection.Chats.UpdateOne(new BsonDocument("_id", chat.Id), Builders<GChat>.Update.Set("LastMenuMessageId", msg.MessageId + 1));

    }

    private static async Task MainMenuButtonsHandler(CallbackQuery query,char buttonNum, GUser user)
    {
        switch(buttonNum)
        {
            case '1':
                await Env.Client.SendTextMessageAsync(query.Message.Chat.Id, "I am virtual assistant GPTStudio. How can I assist you today?");
                break;
            case '2':
                await OpenMenuContent(query.Message, "Settings", KeyboardBuilder.SettingsMenuMarkup);
                break;
            case '3':
                await OpenMenuContent(query.Message,
                       $"📊 User statistics for: @{query.From.Username},{query.From.FirstName}\n│\n├🗓 <b>Member since:</b> {DateTimeOffset.FromUnixTimeSeconds(user.JoinTimestamp)}\n" +
                       $"├🌀 <b>Tokens generated:</b> {user.TotalTokensGenerated}\n└🔁 <b>Total requests:</b> {user.TotalRequests}",new(KeyboardBuilder.BackButton));
                break;
            case '5' when user.IsAdmin == true:
                {
                    using var stream = Utils.Common.StreamFromString(JsonConvert.SerializeObject(Connection.Users.Find(o => o.Id != 0).ToList(),Formatting.Indented));
                    await Env.Client.SendDocumentAsync(query.Message.Chat.Id, new(stream, $"Users {DateTime.Now:yyyy-MM-dd  HH\\;mm\\;ss}.json"), caption: $"👥 Users database for {DateTime.UtcNow:R}");
                    break;
                }
            case '6' when user.IsAdmin == true:
                {
                    using var stream = Utils.Common.StreamFromString(JsonConvert.SerializeObject(Connection.Chats.Find("{}").ToList(), Formatting.Indented));
                    await Env.Client.SendDocumentAsync(query.Message.Chat.Id, new(stream, $"Chats {DateTime.Now:yyyy-MM-dd  HH\\;mm\\;ss}.json"),caption: $"📚 Chats database for {DateTime.UtcNow:R}");
                    break;
                }

        }
    }

    private static async Task SettingsMenuButtonsHandler(CallbackQuery query, char buttonNum)
    {
        switch (buttonNum)
        {

        }
    }

    public static async Task HandleCallbackQuery(CallbackQuery query,GUser user)
    {
        if (query == null) return;

        if (query.Data[0] == '1')
            await MainMenuButtonsHandler(query, query.Data[^1], user);
        else if (query.Data[0] == '2')
            await SettingsMenuButtonsHandler(query, query.Data[^1]);
        else if (query.Data == "back1")
            await OpenMainMenu(query.Message, user);
        else if (query.Data.StartsWith("img"))
            return;
        
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static async Task OpenMainMenu(Message msg, GUser user) =>
        await OpenMenuContent(msg, "📃 Use the buttons <b>below</b> to navigate and setting",
                   user.IsAdmin == true ? KeyboardBuilder.MainAdminMenuMarkup : KeyboardBuilder.MainMenuMarkup);
    
    public static async Task HandleCommand(Message command,GUser user)
    {
     //   GChat.PushMessage(command.Chat.Id, new(command.MessageId, command.Text, user.Id, GMessageType.Command));
        switch (command.Text.Split(' ').First())
        {
            case "/menu" or "/start":
                await OpenMainMenu(command,user);
                break;

            case "/image":
                if (command.Text.Length < 5)
                    return;

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
