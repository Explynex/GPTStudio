using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPTStudio.TelegramProvider.Database.Models;

internal class GUser
{
    public GUser(long userId)
    {
        Id = userId;
        JoinTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    [BsonId]
    public long Id { get; private set; }
    public int TotalRequests { get; set; }
    public int TotalTokensGenerated { get; set; }
    public long JoinTimestamp { get; private set; }

    [BsonIgnoreIfDefault]
    public bool? IsAdmin { get; set; }

    [BsonIgnoreIfNull]
    public string? Username { get; set; }
}
