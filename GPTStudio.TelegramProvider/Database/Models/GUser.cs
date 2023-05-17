using MongoDB.Bson.Serialization.Attributes;

namespace GPTStudio.TelegramProvider.Database.Models;

internal enum ModelMode : byte
{
    Chat,
    Edit,
    Insert,
    Complete
}

internal sealed class GUser
{
    public GUser(long userId)
    {
        Id = userId;
        JoinTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    internal class Quota
    {
        public Quota(int dailyMaxLimit) => this.DailyMax = dailyMaxLimit;
        public int DailyMax { get; set; }
        public int Used { get; set; }
        public long UsedTimestamp { get; set; }
    }

    internal class ChatModelProps
    {
        public ChatModelProps() { }
        public Quota Quota { get; set; }             = new(10000);
        public double Temperature { get; set; }      = 0.7d;
        public int MaxTokens { get; set; }           = 1024;
        public double TopP { get; set; }             = 1d;
        public double FrequencyPenalty { get; set; } = 0d;
        public double PresencePenalty { get; set; }  = 0d;
    }

    [BsonId]
    public long Id { get; private set; }
    public int TotalRequests { get; set; }
    public int TotalTokensGenerated { get; set; }
    public long JoinTimestamp { get; private set; }
    public bool? GenFullyMode { get; set; }

    [BsonIgnoreIfDefault]
    public bool? IsAdmin { get; set; }

    [BsonIgnoreIfNull]
    public string? Username { get; set; }

    [BsonIgnoreIfNull]
    [BsonElement("ChatModel")]
    private ChatModelProps? _chatModel { get; set; }

    [BsonIgnore]
    public ChatModelProps ChatModel => _chatModel ??= new();


    [BsonIgnoreIfNull]
    [BsonElement("LocaleCode")]
    private string? _localeCode;

    public ModelMode Mode { get; set; }

    [BsonIgnore]
    public string LocaleCode
    {
        get => _localeCode ?? "en";
        set => _localeCode = value;
    }
}
