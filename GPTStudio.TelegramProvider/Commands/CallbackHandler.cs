using GPTStudio.TelegramProvider.Database;
using GPTStudio.TelegramProvider.Database.Models;
using GPTStudio.TelegramProvider.Globalization;
using GPTStudio.TelegramProvider.Keyboard;
using GPTStudio.TelegramProvider.Utils;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Runtime.CompilerServices;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Env = GPTStudio.TelegramProvider.Infrastructure.Configuration;

namespace GPTStudio.TelegramProvider.Commands;
internal static class CallbackHandler
{

    public static async Task HandleCallbackQuery(CallbackQuery query, GUser user)
    {
        if (query.Data == null || query.Message == null) return;

        var locale = Locale.Cultures[user.LocaleCode];

        if (!Enum.TryParse(query.Data, out KeyboardCallbackData callback))
        {
            var split = query.Data.Split('.');
            HandleCallbackTextData(query.Message, user, split[0], split[1]);
            return;
        }

        var bsonId = new BsonDocument("_id", user.Id);

        switch (callback)
        {
            #region Mode properties callback
            case KeyboardCallbackData.ModesChatMode:
            case KeyboardCallbackData.ModesCompleteMode:
            case KeyboardCallbackData.ModesInsertMode:
                if (user.SelectedMode == (BotMode)callback) return;
                user.SelectedMode = (BotMode)callback;
                Connection.Users.UpdateOne(new BsonDocument("_id", user.Id), Builders<GUser>.Update.Set(nameof(GUser.SelectedMode), callback));
                await MenuProvider.OpenMenuContent(query.Message, locale[Strings.ModesMenuTitle], KeyboardBuilder.ModesMenuMarkup(user));
                break;

            case KeyboardCallbackData.ModeSettingsMenu:
                await MenuProvider.OpenMenuContent(query.Message, "SelectedMode settings", KeyboardBuilder.ModeSettingsMarkup(user.SelectedMode, user));
                break;

            case KeyboardCallbackData.Tokens:
                await MenuProvider.OpenMenuContent(query.Message, $"Tokens: {user.SelectedModeSettings.MaxTokens}\\{(user.SelectedMode == BotMode.ChatMode ? 2048 : 4000)}", KeyboardBuilder.TokensSettingsMarkup(user.LocaleCode));
                break;

            case KeyboardCallbackData.Temperature or KeyboardCallbackData.FrequencyPenalty or KeyboardCallbackData.PresencePenalty:
                await MenuProvider.OpenMenuContent(query.Message, $"{string.Join(' ', Common.SplitCamelCase(callback.ToString()))}: {Math.Round((double)user.SelectedModeSettings.GetPropertyValue(callback.ToString()), 2)}\\2.0", KeyboardBuilder.FloatKeyboardMarkup(user.LocaleCode, callback.ToString()));
                break;

            case KeyboardCallbackData.SetChatModeSystemMessage:
                var IsMessageExists = !string.IsNullOrEmpty(user.ChatMode.SystemMessage);
                await MenuProvider.OpenMenuContent(query.Message, "🔸 Отправьте сообщение, которое будет служить подсказкой боту при общении\n" +
                    (IsMessageExists ? $"🔰\n🔰 <b>Текущее сообщение которому следует бот</b>: {user.ChatMode.SystemMessage}" : ""),
                    IsMessageExists ? KeyboardBuilder.SetSystemMessageMarkup(user) : KeyboardBuilder.CancelLastCommandButton(user.LocaleCode));

                if (user.LastCommand != WaitCommand.SetChatModeSystemMessage)
                    Connection.Users.UpdateOne(bsonId, Builders<GUser>.Update.Set(nameof(GUser.LastCommand), WaitCommand.SetChatModeSystemMessage));
                break;

            case KeyboardCallbackData.RemoveSystemMessage:
                user.ChatMode.SystemMessage = null;
                CommonHelpers.SetSystemMessage(query.Message, user);
                break;

            case KeyboardCallbackData.IgnoreChatHistory:
                user.ChatMode.IgnoreChatHistory = !user.ChatMode.IgnoreChatHistory;
                await MenuProvider.OpenMenuContent(query.Message, "SelectedMode settings", KeyboardBuilder.ModeSettingsMarkup(user.SelectedMode, user));
                Connection.Users.UpdateOne(bsonId, Builders<GUser>.Update.Set($"{nameof(user.ChatMode)}.{nameof(user.ChatMode.IgnoreChatHistory)}", user.ChatMode.IgnoreChatHistory));
                break;
            #endregion

            #region Menu callback
            case KeyboardCallbackData.MainMenu:
                await MenuProvider.OpenMainMenu(query.Message, user);
                break;

            case KeyboardCallbackData.SettingsMenu:
                await MenuProvider.OpenSettingsMenu(query.Message, user);
                break;

            case KeyboardCallbackData.MainMenuStartChat:
                await Env.Client.SendTextMessageAsync(query.Message.Chat.Id, locale[Strings.StartChattingMsg]);
                break;

            case KeyboardCallbackData.AboutMenu:
                break;

            case KeyboardCallbackData.ServicesMenu:
                await MenuProvider.OpenServicesMenu(query.Message, user);
                break;

            case KeyboardCallbackData.SummaryMenu:
                MenuProvider.OpenSummaryMenu(query, user, locale);
                break;

            case KeyboardCallbackData.ModesMenu:
                await MenuProvider.OpenMenuContent(query.Message, locale[Strings.ModesMenuTitle], KeyboardBuilder.ModesMenuMarkup(user));
                break;

            case KeyboardCallbackData.LanguagesMenu:
                await MenuProvider.OpenMenuContent(query.Message, Locale.Cultures[user.LocaleCode][Strings.LanguagesMenuTitle],
                    KeyboardBuilder.LanguagesMarkup(user.LocaleCode)).ConfigureAwait(false);
                break;

            case KeyboardCallbackData.AdminPanelMenu when user.IsAdmin == true:
                await MenuProvider.OpenMenuContent(query.Message, Locale.Cultures[user.LocaleCode][Strings.AdminPanelTitle],
                    KeyboardBuilder.AdminPanelMarkup(user)).ConfigureAwait(false);
                break;
            #endregion

            case KeyboardCallbackData.SettingsGenMode:
                user.GenFullyMode = !(user.GenFullyMode ?? false);
                Connection.Users.UpdateOne(bsonId, Builders<GUser>.Update.Set(nameof(user.GenFullyMode), user.GenFullyMode));
                await MenuProvider.OpenSettingsMenu(query.Message!, user).ConfigureAwait(false);
                break;

            case KeyboardCallbackData.CancelWaitCommand:
                CommonHelpers.CancelLastCommand(query.Message, user);
                break;

            case KeyboardCallbackData.ImageToTextService:

                if (string.IsNullOrEmpty(Env.Props.Azure.ComputerVisionKey) || string.IsNullOrEmpty(Env.Props.Azure.ComputerVisionServiceName))
                {
                    await Env.Client.SendTextMessageAsync(query.Message.Chat.Id, "🚧 Сервис недоступен, попробуйте позже");
                    return;
                }

                await MenuProvider.OpenImageToTextMenu(query.Message, user).ConfigureAwait(false);

                if (user.LastCommand != WaitCommand.MassRequestFile)
                    Connection.Users.UpdateOne(new BsonDocument("_id", user.Id), Builders<GUser>.Update.Set(nameof(GUser.LastCommand), WaitCommand.ExtractTextFromImage));
                break;

            default:
                if (user.IsAdmin == true)
                    HandleAdminCallback(query.Message, user,callback);

                break;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static async void HandleAdminCallback(Message msg, GUser user, KeyboardCallbackData callback)
    {
        switch(callback)
        {
            case KeyboardCallbackData.MassRequestService:
                await MenuProvider.OpenMenuContent(msg, "📥 Отправьте текстовый документ с несколькими запросами а так же укажите строку-разделитель в строке \"Подпись\".",
                    InlineKeyboardButton.WithCallbackData("Отмена", $"{KeyboardCallbackData.CancelWaitCommand}"));

                if (user.LastCommand != WaitCommand.MassRequestFile)
                    Connection.Users.UpdateOne(new BsonDocument("_id", user.Id), Builders<GUser>.Update.Set(nameof(GUser.LastCommand), WaitCommand.MassRequestFile));
                break;

            case KeyboardCallbackData.RestartBot:
                _ = Task.Run(async () =>
                {
                    await Task.Delay(1000);
                    ConsoleHandler.HandleConsoleCommand("restart");
                });
                
                break;
        }
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static async void HandleCallbackTextData(Message msg, GUser user, string tag, string data)
    {
        var bsonId = new BsonDocument("_id", user.Id);
        switch (tag)
        {
            case "stop":
                App.NowGeneration.Remove(Convert.ToInt64(data));
                break;

            case "lang":
                var lang = data.Split('|');

                if (user.LocaleCode == lang[^1])
                    return;

                Connection.Users.UpdateOne(bsonId, Builders<GUser>.Update.Set(nameof(user.LocaleCode), lang[^1]));
                await Env.Client.SendTextMessageAsync(msg.Chat.Id, $"{Locale.Cultures[lang[^1]][Strings.SuccessChangeLang]}{lang[0]}").ConfigureAwait(false);
                break;

            #region Change mode settings props
            case "tokens":
                var peak = user.SelectedMode == BotMode.ChatMode ? 2048 : 4000;
                var tokens = Convert.ToInt32(data);

                // 3 just key for min and max
                if (tokens == 3) user.SelectedModeSettings.MaxTokens = peak;
                else if (tokens == -3) user.SelectedModeSettings.MaxTokens = 1;
                else user.SelectedModeSettings.MaxTokens += tokens;

                if (user.SelectedModeSettings.MaxTokens < 1 || user.SelectedModeSettings.MaxTokens > peak)
                    return;

                Connection.Users.UpdateOne(bsonId, Builders<GUser>.Update.Set(user.SelectedMode.ToString(), user.SelectedModeSettings));
                await MenuProvider.OpenMenuContent(msg, $"Tokens: {user.SelectedModeSettings.MaxTokens}\\{peak}", KeyboardBuilder.TokensSettingsMarkup(user.LocaleCode));
                break;

            case nameof(GUser.GAbstractMode.PresencePenalty) or nameof(GUser.GAbstractMode.FrequencyPenalty) or nameof(GUser.GAbstractMode.Temperature):

                var value = Convert.ToDouble(data);
                if (value == 3d)
                    value = 2d;
                else if (value == -3d)
                    value = 0d;
                else
                    value += (double)user.SelectedModeSettings.GetPropertyValue(tag);

                user.SelectedModeSettings.SetPropertyValue(tag, value);

                if (value > 2.0 || value < 0d)
                    return;

                value = Math.Round(value, 2);
                Connection.Users.UpdateOne(bsonId, Builders<GUser>.Update.Set(user.SelectedMode.ToString(), user.SelectedModeSettings));
                await MenuProvider.OpenMenuContent(msg, $"{string.Join(' ', Common.SplitCamelCase(tag))}: {value:0.00}\\2,00", KeyboardBuilder.FloatKeyboardMarkup(user.LocaleCode, tag));

                break;
                #endregion
        }

    }

}
