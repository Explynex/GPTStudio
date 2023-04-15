using System.Text.Json.Serialization;

namespace GPTStudio.OpenAI.Chat
{
    public interface IMessage
    {
        [JsonInclude]
        [JsonPropertyName("role")]
        public Role Role { get; }

        [JsonInclude]
        [JsonPropertyName("content")]
        public string Content { get; }
    }
    public sealed class Message : IMessage
    {
        public Message(Role role, string content)
        {
            Role = role;
            Content = content;
        }


        public Role Role { get; private set; }


        public string Content { get; private set; }

        public static implicit operator string(Message message) => message.Content;
    }
}
