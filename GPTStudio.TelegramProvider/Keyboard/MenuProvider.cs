using GPTStudio.TelegramProvider.Database;
using GPTStudio.TelegramProvider.Database.Models;
using GPTStudio.TelegramProvider.Globalization;
using GPTStudio.TelegramProvider.Utils;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Env = GPTStudio.TelegramProvider.Infrastructure.Configuration;

namespace GPTStudio.TelegramProvider.Keyboard;
internal static class MenuProvider
{

    public static async Task OpenMenuContent(Message msg, string subMessage, InlineKeyboardMarkup markup)
    {
        Connection.Chats.FindFirst(new BsonDocument("_id", msg.Chat.Id), out GChat chat);
        try
        {
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
                catch { await Env.Client.EditMessageTextAsync(chat.Id, chat.LastMenuMessageId.Value, "Command expired."); }
            }
        }
        catch { }
        await Env.Client.SendTextMessageAsync(chat.Id, subMessage, ParseMode.Html, replyMarkup: markup);

        Connection.Chats.UpdateOne(new BsonDocument("_id", chat.Id), Builders<GChat>.Update.Set(nameof(GChat.LastMenuMessageId), msg.MessageId + 1));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async Task OpenMainMenu(Message msg, GUser user) =>
    await OpenMenuContent(msg, Locale.Cultures[user.LocaleCode][Strings.MainMenuTitle],
                KeyboardBuilder.MainMenuMarkup(user.LocaleCode, user.IsAdmin)).ConfigureAwait(false);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async Task OpenSettingsMenu(Message msg, GUser user) =>
        await OpenMenuContent(msg, Locale.Cultures[user.LocaleCode][Strings.SettingsTitle],
                            KeyboardBuilder.SettingsMenuMarkup(user)).ConfigureAwait(false);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async Task OpenServicesMenu(Message msg, GUser user) =>
        await OpenMenuContent(msg, Locale.Cultures[user.LocaleCode][Strings.ServicesMenuTitle],
                            KeyboardBuilder.ServicesMenuMarkup(user)).ConfigureAwait(false);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async Task OpenImageToTextMenu(Message msg, GUser user) => 
        await OpenMenuContent(msg, "📥 Отправьте изображение с текстом",
            InlineKeyboardButton.WithCallbackData("Отмена", $"{KeyboardCallbackData.CancelWaitCommand}"));

    public static async void OpenSummaryMenu(CallbackQuery query, GUser user, Dictionary<Strings, string> locale)
    {
        var summaryString = new StringBuilder($"{locale[Strings.SummaryForMsg]} @{query.From.Username},{query.From.FirstName}\n│\n├{locale[Strings.SummaryMemberSince]}\t{DateTimeOffset.FromUnixTimeSeconds(user.JoinTimestamp)}\n")
    .AppendLine($"├🚧 <b>Chat tokens quota:</b> {(user.ChatMode.Quota.DailyMax < 0 ? "Unlimited" : user.ChatMode.Quota.DailyMax)}/day")
    .Append($"├{locale[Strings.SummaryTokensGen]}\t{user.TotalTokensGenerated}\n└{locale[Strings.SummaryRequests]}\t{user.TotalRequests}");

        if (user.IsAdmin == true)
        {
            int totalTokens = 0, totalUsers = 0;
            await Connection.Users.Aggregate().ForEachAsync(o =>
            {
                totalTokens += o.TotalTokensGenerated;
                totalUsers++;
            });

            summaryString.AppendLine($"\n\n┌📈 <b>Uptime:</b> {(DateTime.Now - Process.GetCurrentProcess().StartTime).ToReadableString()}")
                .AppendLine($"├🗂 <b>Total chats:</b> {Connection.Chats.CountDocuments("{}")}")
                .AppendLine($"├👥 <b>Total users:</b> {totalUsers}")
                .AppendLine($"└💠 <b>Total tokens generated:</b> {totalTokens}");
        }


        await OpenMenuContent(query.Message!, summaryString.ToString(), new(KeyboardBuilder.BackToMainButton(user.LocaleCode)));
    }



}
