using GPTStudio.TelegramProvider.Database;
using GPTStudio.TelegramProvider.Database.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Runtime.CompilerServices;
using Telegram.Bot.Types;
using Amazon.Runtime.Internal.Transform;
using System.Security.Cryptography;
using GPTStudio.TelegramProvider.Infrastructure;
using Newtonsoft.Json;
using System.Xml.Linq;

namespace GPTStudio.TelegramProvider.Commands;

internal static class ConsoleHandler
{
    public class ConsoleCommand
    {
        public ConsoleCommand(string semantic)
        {
            Semantic = semantic;
        }
        public string Semantic { get; }
        public Dictionary<string,object?> Params { get; } = new();

        public object? this[string key]
        {
            get
            {
                if(Params.ContainsKey(key))
                    return Params[key];
                return null;
            }
        }
    }
    public static ConsoleCommand?  ParseCommand(string command)
    {
        var split = command.Split(' ').ToList();
        split.RemoveAll(string.IsNullOrEmpty);

        if (split.Count < 1)
            return null;

        Queue<string> chunks = new(split);
        var console = new ConsoleCommand(chunks.Dequeue().ToLower());

        while (chunks.TryDequeue(out string? key))
        {
            key = key.ToLower();
            if(!key.StartsWith("--") && chunks.TryDequeue(out string? value))
            {
                if(long.TryParse(value, out long digValue))
                    console.Params.TryAdd(key, digValue);
                else
                    console.Params.TryAdd(key, value);
            }
            else
                console.Params.TryAdd(key, null);
        }
        return console;
    }

    private static void SetAdminCommand(ConsoleCommand command)
    {
        var name = command["-user"] ?? command["-id"];

        if (name == null)
        {
            Logger.Print("Unknown option. Usage: setAdmin [-id <id>] or [-user <username>]'", color: ConsoleColor.Gray);
            return;
        }
        if (!FindUser(name, out GUser user, out BsonDocument bson)) return;

        if (user.IsAdmin == null)
        {
            Connection.Users.UpdateOne(bson, Builders<GUser>.Update.Set(nameof(user.IsAdmin), true));
            Logger.Print($"Administrator rights granted to user {name}", color: ConsoleColor.Green);
        }
        else
        {
            Connection.Users.UpdateOne(bson, Builders<GUser>.Update.Unset(nameof(user.IsAdmin)));
            Logger.Print($"Administrator rights for user {name} have been revoked.", color: ConsoleColor.Green);
        }
    }

    private static void SetQuotaCommand(ConsoleCommand command)
    {
        var name = command["-user"] ?? command["-id"];
        if (command["-mode"] is not long mode || mode > 3 || mode < 1 || name == null || command["-v"] is not long value)
        {
            Logger.Print("Unknown option. Usage:\n\t\t\tsetQuota ([-id <id>] or [-user <username>])\n\t\t\t\t[-mode <(1 - chat) (2 - complete) (3 - insert)>] [-v <value>]'", color: ConsoleColor.Gray);
            return;
        }
        if (!FindUser(name, out GUser user, out BsonDocument bson)) return;

        var str = $"{(BotMode)mode}.{nameof(GUser.Quota)}.{nameof(GUser.Quota.DailyMax)}";
        Connection.Users.UpdateOne(bson, Builders<GUser>.Update.Set($"{(BotMode)(mode - 1)}.{nameof(GUser.Quota)}.{nameof(GUser.Quota.DailyMax)}", value));

        Logger.Print($"Quota set successfully", color: ConsoleColor.Green);
    }


    public static void HandleConsoleCommand(string cmd)
    {
        var command = ParseCommand(cmd);

        if (command == null)
            return;

        switch (command.Semantic)
        {
            case "setadmin":
                SetAdminCommand(command);
                break;

            case "stop":
                Logger.Print("Shutting down...", color: ConsoleColor.DarkYellow);
                App.Shutdown();
                break;

            case "setquota":
                SetQuotaCommand(command);
                break;

            case "config":
                Logger.Print($"\n\n{JsonConvert.SerializeObject(Config.Props, new JsonSerializerSettings()
                {
                    Formatting = Formatting.Indented,
                })}\n", color: ConsoleColor.Gray);
                break;

            case "setconfigproperty":
                if (command["-k"] is not string key || (command["-v"] is null && !command.Params.ContainsKey("--unset")))
                {
                    Logger.Print("Unknown option. Usage: setConfigProperty [-k <property>] [-v <value>] or [--unset]'", color: ConsoleColor.Gray);
                    return;
                }

                if(!Config.Props.SetPropertyValue(key, command["-v"] ?? default))
                {
                    Logger.Print($"Failed to set value for property '{key}'.", color: ConsoleColor.Red);
                    return;
                }
                    
                Logger.Print($"The value has been set for the property '{key}'.", color: ConsoleColor.Green);
                Config.Save();

                break;

            case "clear":
                Console.Clear();
                    break;

            case "restart":
                Logger.Print("Restarting...", color: ConsoleColor.DarkYellow);
                App.Restart();
                break;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static BsonDocument GetUserId(object? id)
      =>  new(id is long lid ? new BsonElement("_id", lid) : new BsonElement(nameof(User.Username), id as string));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool FindUser(object name,out GUser user, out BsonDocument bson)
    {
        bson = GetUserId(name);
        if (!Connection.Users.FindFirst(bson, out user))
        {
            Logger.Print($"Cannot find '{name}'",color: ConsoleColor.Red);
            return false;
        }
        return true;
    }
}
