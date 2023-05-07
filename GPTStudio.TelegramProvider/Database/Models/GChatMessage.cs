using GPTStudio.OpenAI.Chat;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types.Enums;

namespace GPTStudio.TelegramProvider.Database.Models;
internal enum GMessageType
{
    Text,
    Command,
    Other
}

internal sealed class GChatMessage : IMessage
{
    public GChatMessage(long id,string text,long? senderId,GMessageType messageType = GMessageType.Text)
    {
        Id               = id;
        Content          = text;
        SenderId         = senderId;
        MessageType      = messageType;
        CreatedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    [BsonId]
    public long Id { get; private set; }
    public GMessageType MessageType { get; set; }
    public long? SenderId { get; private set; }
    public int Tokens { get; set; }
    public long CreatedTimestamp { get; private set; }
    public Role Role { get; set; }
    public string Content { get; set; }
}
