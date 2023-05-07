using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Telegram.Bot.Types;

namespace GPTStudio.TelegramProvider.Database.Models;
internal sealed class GChat
{
    public GChat(long id)
    {
        CreatedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        Id = id;
    }

    public void PushMessage(GChatMessage msg) => PushMessage(this.Id, msg);
    

    public static void PushMessage(long id, GChatMessage msg)
    {
        if (Connection.Chats == null)
            throw new InvalidOperationException($"{Connection.Chats} in database is null.");

        Connection.Chats.UpdateOne(o => o.Id == id,
            Builders<GChat>.Update.Push<GChatMessage>("Messages", msg));
    }

    [BsonId]
    public long Id { get; private set; }
    public List<GChatMessage> Messages { get; set; } = new();
    public long CreatedTimestamp { get; private set; }

    [BsonIgnoreIfNull]
    public int? LastMenuMessageId { get; set; }
}
