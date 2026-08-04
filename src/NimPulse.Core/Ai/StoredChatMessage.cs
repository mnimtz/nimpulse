namespace NimPulse.Core.Ai;

/// <summary>
/// One persisted turn of the KI-Coach conversation. Named "Stored..." (not "ChatMessage") to
/// avoid colliding with <c>OpenAI.Chat.ChatMessage</c> — both types are in scope wherever a
/// provider is used, and C# picks the wrong one silently if the names match.
/// </summary>
public class StoredChatMessage
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public ChatMessageRole Role { get; set; }

    public string Content { get; set; } = "";

    public DateTimeOffset CreatedAt { get; set; }
}

public enum ChatMessageRole
{
    User,
    Assistant,
}
