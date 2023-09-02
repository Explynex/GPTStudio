using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using GPTStudio.TelegramProvider.Database.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using Telegram.Bot.Types;
using GPTStudio.TelegramProvider.Utils;
using GPTStudio.TelegramProvider.Infrastructure;

namespace GPTStudio.TelegramProvider.Database;
internal static class Connection
{
    public static MongoClient Client { get; private set; }
    public static IMongoDatabase Database { get; private set; }
    public static IMongoCollection<GUser> Users { get; private set; }
    public static IMongoCollection<GChat> Chats { get; private set; }

    public static void Connect()
    {
        Logger.Print($"Connection to database {Configuration.Props.DatabaseEndpoint}");
        try
        {
            Client = new MongoClient(Configuration.Props.DatabaseEndpoint);
            Logger.Print($"Getting database collections.");
            Database = Client.GetDatabase("GPTStudio");
            Users = Database.GetCollection<GUser>("Users");
            Chats = Database.GetCollection<GChat>("Chats");
        }
        catch(MongoConfigurationException e)
        {
            if(e.Message.EndsWith("is not valid."))
            {
                Logger.PrintError(" Invalid MongoDB Connection string");
                Configuration.Props.DatabaseEndpoint = null;
                Configuration.Save();
                App.Restart();
            }
                
        }

    }

    public static bool FindFirst<T>(this IMongoCollection<T> collection, Expression<Func<T, bool>> filter,out T element)
    {
        element = collection.Find(filter).Limit(1).FirstOrDefault();
        return element != null;
    }

    public static bool FindFirst<T>(this IMongoCollection<T> collection, FilterDefinition<T> filter, out T element)
    {
        element = collection.Find(filter).Limit(1).FirstOrDefault();
        return element != null;
    }
}
