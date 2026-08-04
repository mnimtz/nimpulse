namespace NimPulse.Core.Ai;

/// <summary>One prior turn in a conversation — Role is "user" or "assistant".</summary>
public record ChatTurn(string Role, string Content);

public interface IAiProvider
{
    string Name { get; }

    /// <summary>
    /// <paramref name="history"/> is the conversation so far (oldest first, without the current
    /// <paramref name="userMessage"/>) — lets the KI-Coach actually remember what was said before
    /// instead of treating every message as an isolated Q&amp;A.
    /// </summary>
    Task<string> AskAsync(string systemPrompt, IReadOnlyList<ChatTurn> history, string userMessage, CancellationToken cancellationToken = default);
}
