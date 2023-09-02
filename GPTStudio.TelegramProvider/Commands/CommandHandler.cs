using GPTStudio.TelegramProvider.Database;
using GPTStudio.TelegramProvider.Database.Models;
using GPTStudio.TelegramProvider.Keyboard;
using GPTStudio.TelegramProvider.Utils;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Env = GPTStudio.TelegramProvider.Infrastructure.Configuration;

namespace GPTStudio.TelegramProvider.Commands;
internal static class CommandHandler
{
    public static async void HandleWaitCommand(Message msg, GUser user)
    {
        if (msg is null)
            return;

        using var downloadStream = new MemoryStream();

        switch (user.LastCommand)
        {
            case WaitCommand.SetChatModeSystemMessage:

                if (string.IsNullOrEmpty(msg.Text))
                    return;

                user.ChatMode.SystemMessage = msg.Text;

                CommonHelpers.SetSystemMessage(msg, user);
                break;

            case WaitCommand.ExtractTextFromImage when msg.Type == MessageType.Photo:

                await Common.DownloadFileToStream(msg.Photo![^1].FileId, downloadStream);

                try
                {
                    var result = await Common.ExtractTextFromImage(downloadStream);

                    if(string.IsNullOrEmpty(result))
                       await Env.Client.SendTextMessageAsync(msg.Chat.Id, "✴️ Не удалось распознать текст на изображении",replyToMessageId:msg.MessageId);
                    else
                       await Env.Client.SendTextMessageAsync(msg.Chat.Id, $"✅ *Текст распознан*\n\n`{result}`", replyToMessageId: msg.MessageId,parseMode: ParseMode.Markdown);
                }
                catch (Exception e)
                {
                    Common.NotifyOfRequestError(msg.Chat.Id, user, e, replyMsgId: msg.MessageId);
                }
                break;


            case WaitCommand.MassRequestFile when msg.Type == MessageType.Document && user.IsAdmin == true:
                {
                    if (string.IsNullOrEmpty(msg.Caption))
                    {
                        await Env.Client.SendTextMessageAsync(msg.Chat.Id, "❌ Ошибка: не указан разделитель");
                        return;
                    }

                    if (!await IsValidFileSize(msg.Document!.FileSize!.Value,128000))
                        return;

                    await Common.DownloadFileToStream(msg.Document.FileId, downloadStream);

                    var stream  = new StreamReader(downloadStream);
                    var content = await stream.ReadToEndAsync();

                    var requests = content.Split(msg.Caption);

                    if (requests == null || requests.Length == 0)
                    {
                        await Env.Client.SendTextMessageAsync(msg.Chat.Id, "❌ Ошибка: Не найдено совпадений");
                        return;
                    }

                    int i                  = 0;
                    StringBuilder response = new();
                    StringBuilder temp     = new();
                    var stateMsg           = await Env.Client.SendTextMessageAsync(msg.Chat.Id, $"🔸 Найдено запросов: {requests.Length}");
                    int totalTokens        = 0;

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
                    await MenuProvider.OpenServicesMenu(msg,user);

                    break;
                }
            default:
                await Env.Client.SendTextMessageAsync(msg.Chat.Id, "❗️ Введите подходящие данные или отмените команду: `/cancel`", parseMode: ParseMode.Markdown,
                    replyMarkup: new ReplyKeyboardMarkup(new KeyboardButton("/cancel")) { ResizeKeyboard = true, OneTimeKeyboard = true});
                break;
        }

        async Task<bool> IsValidFileSize(long fileSize, long peak)
        {
            if (fileSize > peak)
                return true;

            await Env.Client.SendTextMessageAsync(msg.Chat.Id, "❌ Ошибка: файл слишком большой");
            return false;
        }
    }

    public static async Task HandleCommand(Message command,GUser user)
    {
        switch (command.Text!.Split(' ').First())
        {
            case "/menu" or "/start":
                await MenuProvider.OpenMainMenu(command,user).ConfigureAwait(false);
                break;

            case "/cancel":
                CommonHelpers.CancelLastCommand(command, user,true);
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
