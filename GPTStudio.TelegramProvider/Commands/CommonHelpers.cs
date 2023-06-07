using GPTStudio.TelegramProvider.Database;
using GPTStudio.TelegramProvider.Database.Models;
using GPTStudio.TelegramProvider.Globalization;
using GPTStudio.TelegramProvider.Keyboard;
using GPTStudio.TelegramProvider.Utils;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using System.Runtime.CompilerServices;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Env = GPTStudio.TelegramProvider.Infrastructure.Configuration;

namespace GPTStudio.TelegramProvider.Commands;
internal static class CommonHelpers
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async void SendChatsDb(long chatId)
    {
        using var stream = Common.StreamFromString(JsonConvert.SerializeObject(Connection.Chats.Find("{}").ToList(), Formatting.Indented));
        await Env.Client.SendDocumentAsync(chatId, new(stream, $"Chats {DateTime.Now:yyyy-MM-dd  HH\\;mm\\;ss}.json"), caption: $"📚 Chats database for {DateTime.UtcNow:R}");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async void SendUsersDb(long chatId)
    {
        using var stream = Common.StreamFromString(JsonConvert.SerializeObject(Connection.Users.Find(o => o.Id != 0).ToList(), Formatting.Indented));
        await Env.Client.SendDocumentAsync(chatId, new(stream, $"Users {DateTime.Now:yyyy-MM-dd  HH\\;mm\\;ss}.json"), caption: $"👥 Users database for {DateTime.UtcNow:R}");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async void CancelLastCommand(Message command, GUser user,bool verbose = false)
    {
        if (user.LastCommand == WaitCommand.None)
            return;

        Connection.Users.UpdateOne(new BsonDocument("_id", user.Id), Builders<GUser>.Update.Unset(nameof(GUser.LastCommand)));
        if(verbose)
             await Env.Client.SendTextMessageAsync(command.Chat.Id, "🔸 Последня команда ожидающая ответа отменена");
        await MenuProvider.OpenMainMenu(command, user);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async void SetSystemMessage(Message msg, GUser user)
    {
        user.ResetLastCommand();
        Connection.Users.UpdateOne(new BsonDocument("_id", user.Id), Builders<GUser>.Update.Set(user.SelectedMode.ToString(), user.SelectedModeSettings));
        await MenuProvider.OpenMenuContent(msg, "SelectedMode settings", KeyboardBuilder.ModeSettingsMarkup(user.SelectedMode, user.LocaleCode));
    }
    
}
