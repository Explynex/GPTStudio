using GPTStudio.TelegramProvider.Database;
using GPTStudio.TelegramProvider.Database.Models;
using GPTStudio.TelegramProvider.Utils;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Runtime.CompilerServices;
using Telegram.Bot.Types;

namespace GPTStudio.TelegramProvider.Commands;
internal static class ConsoleHandler
{
    public static void HandleConsoleCommand(string cmd)
    {
        var chunk = cmd.Split(' ');

        switch (chunk[0].ToLower())
        {
            case "setadmin":
                
                if (chunk.Length != 2)
                {
                    Logger.PrintError("Invalid syntax. Must be 'setadmin <id or username>'");
                    return;
                }

                var bson = GetUserId(chunk[^1]);
                
                if(!Connection.Users.FindFirst(bson, out GUser user))
                {
                    Logger.PrintError($"Cannot find '{chunk[^1]}'");
                    return;
                }

                if(user.IsAdmin == null)
                {
                    Connection.Users.UpdateOne(bson, Builders<GUser>.Update.Set(nameof(user.IsAdmin), true));
                    Logger.Print($"Administrator rights granted to user {chunk[^1]}", color: ConsoleColor.Green);
                }
                else
                {
                    Connection.Users.UpdateOne(bson, Builders<GUser>.Update.Unset(nameof(user.IsAdmin)));
                    Logger.Print($"Administrator rights for user {chunk[^1]} have been revoked.",color: ConsoleColor.Green);
                }
                    
                break;

            case "stop":
                Logger.Print("Shutting down...", color: ConsoleColor.DarkYellow);
                App.Shutdown();
                break;

            case "setquota":
                string? modelName = null;
                if(chunk.Length != 4 || !chunk[1].All(char.IsDigit) || !Common.Integer().IsMatch(chunk[^1]) || (modelName = GetModeName(Convert.ToInt32(chunk[1]))) == null)
                {
                    Logger.PrintError("Invalid syntax. Must be 'setquota <mode number (1 - chat, 2 - complete,3 - insert)> <id or username> <value>'");
                    return;
                }

                bson = GetUserId(chunk[^2]);
                if (!Connection.Users.FindFirst(bson, out user))
                {
                    Logger.PrintError($"Cannot find '{chunk[^2]}'");
                    return;
                }

                user.ChatMode.Quota.DailyMax = Convert.ToInt32(chunk[^1]);
                Connection.Users.UpdateOne(bson, Builders<GUser>.Update.Set(modelName, user.ChatMode));
                Logger.Print($"Quota set successfully", color: ConsoleColor.Green);

                break;

            case "restart":
                Logger.Print("Restarting...", color: ConsoleColor.DarkYellow);
                Common.ExecConsoleCommand($"\"{Environment.ProcessPath}\"",3);
                App.Shutdown();
                break;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static BsonDocument GetUserId(string id) =>
        new(id.All(char.IsDigit) ? new BsonElement("_id", long.Parse(id)) : new BsonElement(nameof(User.Username), id));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string? GetModeName(int id) => id switch
    {
        1 => nameof(GUser.ChatMode),
        2 => nameof(GUser.CompleteMode),
        3 => nameof(GUser.InsertMode),
        _ => null
    };
}
