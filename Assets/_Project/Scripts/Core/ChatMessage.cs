namespace FarmSimVR.Core
{
    /// <summary>
    /// Lightweight DTO that represents a single message in an LLM conversation.
    /// </summary>
    public class ChatMessage
    {
        public string Role { get; }
        public string Content { get; }

        public ChatMessage(string role, string content)
        {
            Role = role;
            Content = content;
        }
    }
}
