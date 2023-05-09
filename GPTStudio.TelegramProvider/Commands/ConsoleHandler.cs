using GPTStudio.TelegramProvider.Database;
using GPTStudio.TelegramProvider.Database.Models;
using GPTStudio.TelegramProvider.Utils;
using MongoDB.Bson;
using MongoDB.Driver;
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

                var id = chunk[^1];
                var bson = new BsonDocument(id.All(char.IsDigit) ? new BsonElement("_id",long.Parse(id)) : new BsonElement(nameof(User.Username),id));
                
                if(!Connection.Users.FindFirst(bson, out GUser user))
                {
                    Logger.PrintError($"Cannot find '{id}'");
                    return;
                }

                if(user.IsAdmin == null)
                {
                    Connection.Users.UpdateOne(bson, Builders<GUser>.Update.Set(nameof(user.IsAdmin), true));
                    Logger.Print($"Administrator rights granted to user {id}", color: ConsoleColor.Green);
                }
                else
                {
                    Connection.Users.UpdateOne(bson, Builders<GUser>.Update.Unset(nameof(user.IsAdmin)));
                    Logger.Print($"Administrator rights for user {id} have been revoked.",color: ConsoleColor.Green);
                }
                    
                break;

            case "stop":
                Logger.Print("Shutting down...", color: ConsoleColor.DarkYellow);
                App.IsShuttingDown = true;
                break;
        }
    }
}
