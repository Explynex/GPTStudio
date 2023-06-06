using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace GPTStudio.TelegramProvider.Database.Models;

//db.Users.updateMany({}, { $unset: { ChatModel: null}})
internal enum ModelMode : byte
{
    ChatMode,
    InsertMode,
    CompleteMode,
}

internal enum WaitCommand : byte
{
    None,
    SetChatModeSystemMessage,
    MassRequestFile,
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

    #region Mode classes

    internal abstract class GAbstractMode
    {
        public abstract Quota Quota { get; set; }
        public virtual double Temperature { get; set; } = 0.7d;
        public virtual int MaxTokens { get; set; } = 2048;
        public virtual double TopP { get; set; } = 1d;
        public virtual double FrequencyPenalty { get; set; } = 0d;
        public virtual double PresencePenalty { get; set; } = 0d;
    }

    internal class GChatMode : GAbstractMode
    {
        public override Quota Quota { get; set; } = new(12000);
        public string? SystemMessage { get; set; }
    }

    internal class GCompleteMode : GAbstractMode
    {
        public override Quota Quota { get; set; } = new(8000);
        public string? InjectStartText { get; set; }
        public string? InjectRestartText { get; set; }
        public int BestOf { get; set; } = 1;
    }

    internal class GInsertMode : GAbstractMode
    {
        public override Quota Quota { get; set; } = new(10000);
    } 
    #endregion

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
    [BsonElement("ChatMode")]
    private GChatMode? _chatModel { get; set; }
    [BsonIgnore]
    public GChatMode ChatMode => _chatModel ??= new();


    [BsonIgnoreIfNull]
    [BsonElement("CompleteMode")]
    private GCompleteMode? _completeMode { get; set; }
    [BsonIgnore]
    public GCompleteMode CompleteMode => _completeMode ??= new();


    [BsonIgnoreIfNull]
    [BsonElement("InsertMode")]
    private GInsertMode? _insertMode { get; set; }
    [BsonIgnore]
    public GInsertMode InsertMode => _insertMode ??= new();


    [BsonIgnoreIfNull]
    [BsonElement("LocaleCode")]
    private string? _localeCode;

    public ModelMode SelectedMode { get; set; }
    public WaitCommand LastCommand { get; set; }

    [BsonIgnore]
    public GAbstractMode SelectedModeSettings => SelectedMode == ModelMode.ChatMode 
        ? ChatMode : SelectedMode == ModelMode.InsertMode ? InsertMode : CompleteMode;



    [BsonIgnore]
    public string LocaleCode
    {
        get => _localeCode ?? "en";
        set => _localeCode = value;
    }

    public void ResetLastCommand()
        => Connection.Users.UpdateOne(o => o.Id == Id, Builders<GUser>.Update.Unset(nameof(LastCommand)));
}
