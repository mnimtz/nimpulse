namespace NimPulse.Core.Ai;

public interface IAiProvider
{
    string Name { get; }

    Task<string> AskAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken = default);
}
