using GPTStudio.TelegramProvider.Database;
using GPTStudio.TelegramProvider.Database.Models;
using GPTStudio.TelegramProvider.Globalization;
using GPTStudio.TelegramProvider.Utils;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using System.Runtime.CompilerServices;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Env = GPTStudio.TelegramProvider.Infrastructure.Configuration;

namespace GPTStudio.TelegramProvider.Commands;
internal static class CommandHandler
{
    #region Helpers
    private static async Task OpenMenuContent(Message msg, string subMessage, InlineKeyboardMarkup markup)
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

    private static async void OpenSummaryMenu(CallbackQuery query, GUser user, Dictionary<Strings, string> locale)
    {
        var summaryString = new StringBuilder($"{locale[Strings.SummaryForMsg]} @{query.From.Username},{query.From.FirstName}\n│\n├{locale[Strings.SummaryMemberSince]}\t{DateTimeOffset.FromUnixTimeSeconds(user.JoinTimestamp)}\n")
    .AppendLine($"├🚧 <b>Chat tokens quota:</b> {(user.ChatMode.Quota.DailyMax < 0 ? "Unlimited" : user.ChatMode.Quota.DailyMax)}/day")
    .Append($"├{locale[Strings.SummaryTokensGen]}\t{user.TotalTokensGenerated}\n└{locale[Strings.SummaryRequests]}\t{user.TotalRequests}");

        if (user.IsAdmin == true)
        {
            int totalTokens = 0, totalChats = 0;
            await Connection.Chats.Aggregate().ForEachAsync(o =>
            {
                totalTokens += o.Messages.Sum(o => o.Role == OpenAI.Chat.Role.Assistant ? o.Tokens : 0);
                totalChats++;
            });

            summaryString.AppendLine($"\n\n┌🆔 <b>Chat ID:</b> {query.Message!.Chat.Id}")
                .AppendLine($"├🗂 <b>Total chats:</b> {totalChats}")
                .AppendLine($"├👥 <b>Total users:</b> {Connection.Users.CountDocuments("{}")}")
                .AppendLine($"└💠 <b>Total tokens generated:</b> {totalTokens}");
        }


        await OpenMenuContent(query.Message!, summaryString.ToString(), new(KeyboardBuilder.BackToMainButton(user.LocaleCode)));
    }

    private static async void SendChatsDb(long chatId)
    {
        using var stream = Common.StreamFromString(JsonConvert.SerializeObject(Connection.Chats.Find("{}").ToList(), Formatting.Indented));
        await Env.Client.SendDocumentAsync(chatId, new(stream, $"Chats {DateTime.Now:yyyy-MM-dd  HH\\;mm\\;ss}.json"), caption: $"📚 Chats database for {DateTime.UtcNow:R}");
    }

    private static async void SendUsersDb(long chatId)
    {
        using var stream = Common.StreamFromString(JsonConvert.SerializeObject(Connection.Users.Find(o => o.Id != 0).ToList(), Formatting.Indented));
        await Env.Client.SendDocumentAsync(chatId, new(stream, $"Users {DateTime.Now:yyyy-MM-dd  HH\\;mm\\;ss}.json"), caption: $"👥 Users database for {DateTime.UtcNow:R}");
    } 
    #endregion


    public static async Task HandleCallbackQuery(CallbackQuery query,GUser user)
    {
        if (query.Data == null || query.Message == null) return;

        var locale = Locale.Cultures[user.LocaleCode];

        if (!Enum.TryParse(query.Data,out KeyboardCallbackData callback))
        {
            var split = query.Data.Split('.');
            HandleCallbackTextData(query.Message, user, split[0], split[1]);
            return;
        }

        switch (callback)
        {
            case KeyboardCallbackData.ModesChatMode:
            case KeyboardCallbackData.ModesCompleteMode:
            case KeyboardCallbackData.ModesInsertMode:
                if (user.SelectedMode == (ModelMode)callback) return;
                user.SelectedMode = (ModelMode)callback;
                Connection.Users.UpdateOne(new BsonDocument("_id", user.Id), Builders<GUser>.Update.Set(nameof(GUser.SelectedMode), callback));
                await OpenMenuContent(query.Message, locale[Strings.ModesMenuTitle], KeyboardBuilder.ModesMenuMarkup(user));
                break;

            case KeyboardCallbackData.ModeSettingsMenu:
                await OpenMenuContent(query.Message, "SelectedMode settings", KeyboardBuilder.ModeSettingsMarkup(user.SelectedMode, user.LocaleCode));
                break;

            case KeyboardCallbackData.Tokens:
                await OpenMenuContent(query.Message, $"Tokens: {user.SelectedModeSettings.MaxTokens}\\{(user.SelectedMode == ModelMode.ChatMode ? 2048 : 4000)}", KeyboardBuilder.TokensSettingsMarkup(user.LocaleCode));
                break;

            case KeyboardCallbackData.Temperature or KeyboardCallbackData.FrequencyPenalty or KeyboardCallbackData.PresencePenalty:
                await OpenMenuContent(query.Message, $"{string.Join(' ', Common.SplitCamelCase(callback.ToString()))}: {Math.Round((double)user.SelectedModeSettings.GetPropertyValue(callback.ToString()),2)}\\2.0", KeyboardBuilder.FloatKeyboardMarkup(user.LocaleCode,callback.ToString()));
                break;

            case KeyboardCallbackData.SetChatModeSystemMessage:
                await OpenMenuContent(query.Message, "▫️ Отправьте сообщение, которому будет следовать бот при общении",
                    InlineKeyboardButton.WithCallbackData("Отмена", $"{KeyboardCallbackData.CancelWaitCommand}"));

                if (user.LastCommand != WaitCommand.SetChatModeSystemMessage)
                    Connection.Users.UpdateOne(new BsonDocument("_id", user.Id), Builders<GUser>.Update.Set(nameof(GUser.LastCommand), WaitCommand.SetChatModeSystemMessage));
                break;
                    
            case KeyboardCallbackData.MainMenu:
                await OpenMainMenu(query.Message, user);
                break;

            case KeyboardCallbackData.SettingsMenu:
                await OpensSettingsMenu(query.Message, user);
                break;

            case KeyboardCallbackData.MainMenuStartChat:
                await Env.Client.SendTextMessageAsync(query.Message!.Chat.Id, locale[Strings.StartChattingMsg]);
                break;

            case KeyboardCallbackData.AboutMenu:
                break;

            case KeyboardCallbackData.SummaryMenu:
                OpenSummaryMenu(query, user, locale);
                break;

            case KeyboardCallbackData.SettingsGenMode:
                user.GenFullyMode = !(user.GenFullyMode ?? false);
                Connection.Users.UpdateOne(new BsonDocument("_id", user.Id), Builders<GUser>.Update.Set(nameof(user.GenFullyMode), user.GenFullyMode));
                await OpensSettingsMenu(query.Message!, user).ConfigureAwait(false);
                break;

            case KeyboardCallbackData.ModesMenu:
                await OpenMenuContent(query.Message, locale[Strings.ModesMenuTitle], KeyboardBuilder.ModesMenuMarkup(user));
                break;

            case KeyboardCallbackData.LanguagesMenu:
                await OpenMenuContent(query.Message, Locale.Cultures[user.LocaleCode][Strings.LanguagesMenuTitle],
                    KeyboardBuilder.LanguagesMarkup(user.LocaleCode)).ConfigureAwait(false);
                break;

            case KeyboardCallbackData.AdminPanelMenu when user.IsAdmin == true:
                await OpenMenuContent(query.Message, Locale.Cultures[user.LocaleCode][Strings.AdminPanelTitle],
                    KeyboardBuilder.AdminPanelMarkup(user)).ConfigureAwait(false);
                break;

            case KeyboardCallbackData.MassRequest when user.IsAdmin == true:
                await OpenMenuContent(query.Message, "📥 Отправьте текстовый документ с несколькими запросами а так же укажите строку-разделитель в строке \"Подпись\".",
                    InlineKeyboardButton.WithCallbackData("Отмена", $"{KeyboardCallbackData.CancelWaitCommand}"));

                if (user.LastCommand != WaitCommand.MassRequestFile)
                    Connection.Users.UpdateOne(new BsonDocument("_id", user.Id), Builders<GUser>.Update.Set(nameof(GUser.LastCommand), WaitCommand.MassRequestFile));
               
                break;

            case KeyboardCallbackData.AdminTotalChats when user.IsAdmin == true:
                SendChatsDb(query.Message.Chat.Id);
                break;

            case KeyboardCallbackData.AdminTotalUsers when user.IsAdmin == true:
                SendUsersDb(query.Message.Chat.Id);
                break;
        }


    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static async void HandleCallbackTextData(Message msg,GUser user,string tag, string data)
    {
        var bsonId = new BsonDocument("_id", user.Id);
        switch (tag)
        {
            case "stop":
                App.NowGeneration.Remove(Convert.ToInt64(data));
                break;

            case "tokens":
                var peak = user.SelectedMode == ModelMode.ChatMode ? 2048 : 4000;
                var tokens = Convert.ToInt32(data);

                if (tokens == 3) user.SelectedModeSettings.MaxTokens = peak;
                else if (tokens == -3) user.SelectedModeSettings.MaxTokens = 1;
                else user.SelectedModeSettings.MaxTokens += tokens;

                if (user.SelectedModeSettings.MaxTokens < 1 || user.SelectedModeSettings.MaxTokens > peak)
                    return;

                Connection.Users.UpdateOne(bsonId, Builders<GUser>.Update.Set(user.SelectedMode.ToString(), user.SelectedModeSettings));
                await OpenMenuContent(msg, $"Tokens: {user.SelectedModeSettings.MaxTokens}\\{peak}", KeyboardBuilder.TokensSettingsMarkup(user.LocaleCode));
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
                await OpenMenuContent(msg, $"{string.Join(' ',Common.SplitCamelCase(tag))}: {value:0.00}\\2,00", KeyboardBuilder.FloatKeyboardMarkup(user.LocaleCode,tag));

                break;

            case "lang":
                var lang = data.Split('|');

                if (user.LocaleCode == lang[^1])
                    return;

                Connection.Users.UpdateOne(bsonId, Builders<GUser>.Update.Set(nameof(user.LocaleCode), lang[^1]));
                await Env.Client.SendTextMessageAsync(msg.Chat.Id, $"{Locale.Cultures[lang[^1]][Strings.SuccessChangeLang]}{lang[0]}").ConfigureAwait(false);
                break;

        }

    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static async Task OpenMainMenu(Message msg, GUser user) =>
        await OpenMenuContent(msg, Locale.Cultures[user.LocaleCode][Strings.MainMenuTitle],
                    KeyboardBuilder.MainMenuMarkup(user.LocaleCode,user.IsAdmin)).ConfigureAwait(false);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static async Task OpensSettingsMenu(Message msg, GUser user) =>
        await OpenMenuContent(msg, Locale.Cultures[user.LocaleCode][Strings.SettingsTitle],
                            KeyboardBuilder.SettingsMenuMarkup(user)).ConfigureAwait(false);

    public static async void HandleWaitCommand(Message msg, GUser user)
    {
        if (msg is null)
            return;

        switch (user.LastCommand)
        {
            case WaitCommand.SetChatModeSystemMessage:

                if (string.IsNullOrEmpty(msg.Text))
                    return;

                user.ChatMode.SystemMessage = msg.Text;

                user.ResetLastCommand();
                Connection.Users.UpdateOne(new BsonDocument("_id",user.Id), Builders<GUser>.Update.Set(user.SelectedMode.ToString(), user.SelectedModeSettings));
                await OpenMenuContent(msg, "SelectedMode settings", KeyboardBuilder.ModeSettingsMarkup(user.SelectedMode, user.LocaleCode));
                break;
            case WaitCommand.MassRequestFile when msg.Type == MessageType.Document && user.IsAdmin == true:
                {
                    if (string.IsNullOrEmpty(msg.Caption))
                    {
                        await Env.Client.SendTextMessageAsync(msg.Chat.Id, "❌ Ошибка: не указан разделитель");
                        return;
                    }

                    if (msg.Document!.FileSize > 128000)
                    {
                        await Env.Client.SendTextMessageAsync(msg.Chat.Id, "❌ Ошибка: файл слишком большой");
                        return;
                    }

                    using var downloadStream = new MemoryStream();
                    await Env.Client.DownloadFileAsync((await Env.Client.GetFileAsync(msg.Document.FileId).ConfigureAwait(false)).FilePath!, downloadStream).ConfigureAwait(false);
                    downloadStream.Position = 0;

                    var stream  = new StreamReader(downloadStream);
                    var content = await stream.ReadToEndAsync();

                    var requests = content.Split(msg.Caption);

                    if (requests == null || requests.Length == 0)
                    {
                        await Env.Client.SendTextMessageAsync(msg.Chat.Id, "❌ Ошибка: Не найдено совпадений");
                        return;
                    }

                    int i = 0;
                    StringBuilder response = new();
                    StringBuilder temp = new();
                    var stateMsg = await Env.Client.SendTextMessageAsync(msg.Chat.Id, $"🔸 Найдено запросов: {requests.Length}");
                    int totalTokens = 0;

                    for (int errCounter = 0; i < requests.Length; i++)
                    {
                        _ = Env.Client.EditMessageTextAsync(stateMsg.Chat.Id,stateMsg.MessageId, $"{stateMsg.Text}\n🌀 Обработка: {i + 1}/{requests.Length}");
                        try
                        {
                            var request = new OpenAI.Chat.ChatRequest(new OpenAI.Chat.Message[]
                                {
                                    new OpenAI.Chat.Message(OpenAI.Chat.Role.System, user.ChatMode.SystemMessage),
                                    new OpenAI.Chat.Message(OpenAI.Chat.Role.User, requests[i])
                                }, maxTokens: user.ChatMode.MaxTokens);

                            await Env.GPTClient.ChatEndpoint.StreamCompletionAsync(request, result =>
                            {
                                if (string.IsNullOrEmpty(result.FirstChoice))
                                    return;
                                temp.Append(result.FirstChoice);
                            });

                            totalTokens += Env.Tokenizer.Calculate(temp.ToString());
                            response.Append($"\n### {i + 1}. {requests[i]}\n").Append(temp).Append("\n\n---\n");
                            temp.Clear();
                            await Task.Delay(20000);
                            errCounter = 0;
                        }
                        catch (Exception e)
                        {
                            if (errCounter < 3)
                            {
                                i--;
                                await Task.Delay(20000);
                                errCounter++;
                            }
                            else
                            {
                                response.Append("Error\n\n---\n");
                                errCounter = 0;
                            }
                                
                        }
                    }
                    
                    

                    using var textStream = Common.StreamFromString(response.ToString());
                    await Env.Client.SendDocumentAsync(msg.Chat.Id, new(textStream, $"Responses to {msg.Document.FileName}.md"));

                    var userBson = new BsonDocument("_id", user.Id);
                    Connection.Users.UpdateOne(userBson, Builders<GUser>.Update.Inc("TotalTokensGenerated", totalTokens));
                    Connection.Users.UpdateOne(userBson, Builders<GUser>.Update.Inc("TotalRequests", i+1));

                    user.ResetLastCommand();

                   

                    break;
                }

        }
    }

    public static async Task HandleCommand(Message command,GUser user)
    {
        switch (command.Text!.Split(' ').First())
        {
            case "/menu" or "/start":
                await OpenMainMenu(command,user).ConfigureAwait(false);
                break;

            case "/cancel" when user.LastCommand != WaitCommand.None:
                Connection.Users.UpdateOne(new BsonDocument("_id", user.Id), Builders<GUser>.Update.Unset(nameof(GUser.LastCommand)));
                await Env.Client.SendTextMessageAsync(command.Chat.Id, "🔸 Последня команда ожидающая ответа отменена");
                break;

            case "/image":
                await Env.Client.SendTextMessageAsync(command.Chat.Id,"❌ Unavailable");
                return;
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
