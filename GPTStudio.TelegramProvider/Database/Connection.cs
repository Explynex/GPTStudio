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
using Env = GPTStudio.TelegramProvider.Infrastructure.Configuration;

namespace GPTStudio.TelegramProvider.Database;
internal static class Connection
{
    public static MongoClient Client { get; private set; }
    public static IMongoDatabase Database { get; private set; }
    public static IMongoCollection<GUser> Users { get; private set; }
    public static IMongoCollection<GChat> Chats { get; private set; }

    public static void Connect()
    {
        Logger.Print($"Connection to database {Env.DatabaseEndpoint}");
        Client   = new MongoClient(Env.DatabaseEndpoint);
        Database = Client.GetDatabase("GPTStudio");
        Logger.Print($"Getting database collections.");
        Users    = Connection.Database.GetCollection<GUser>("Users");
        Chats    = Connection.Database.GetCollection<GChat>("Chats");
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
