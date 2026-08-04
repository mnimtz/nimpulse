using System.ClientModel;
using Microsoft.Extensions.Options;
using NimPulse.Core.Health;
using OpenAI;
using OpenAI.Chat;

namespace NimPulse.Core.Ai;

/// <summary>
/// Plain OpenAI API (not Azure) — for admins with their own OpenAI account/key, no Azure resource
/// needed. Scoped, key/model resolved lazily inside <see cref="AskAsync"/> from the DB-backed
/// KI-Gateway settings, falling back to appsettings/env — same pattern as the other two providers.
/// </summary>
public class OpenAiProvider(IOptions<AiOptions> options, NimPulseDbContext db) : IAiProvider
{
    public string Name => "openai";

    public async Task<string> AskAsync(string systemPrompt, IReadOnlyList<ChatTurn> history, string userMessage, CancellationToken cancellationToken = default)
    {
        var settings = await db.AiGatewaySettings.FindAsync([1], cancellationToken);
        var fallback = options.Value.OpenAi;
        var apiKey = string.IsNullOrWhiteSpace(settings?.OpenAiApiKey) ? fallback.ApiKey : settings.OpenAiApiKey;
        var model = string.IsNullOrWhiteSpace(settings?.OpenAiModel) ? fallback.Model : settings.OpenAiModel;

        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(model))
        {
            throw new InvalidOperationException("OpenAI ist nicht konfiguriert — API-Key und Modell im KI-Gateway (Einstellungen) setzen.");
        }

        var client = new OpenAIClient(new ApiKeyCredential(apiKey));
        var chatClient = client.GetChatClient(model);

        var messages = new List<ChatMessage> { new SystemChatMessage(systemPrompt) };
        messages.AddRange(history.Select(turn => turn.Role == "user" ? (ChatMessage)new UserChatMessage(turn.Content) : new AssistantChatMessage(turn.Content)));
        messages.Add(new UserChatMessage(userMessage));

        var response = await chatClient.CompleteChatAsync(messages, cancellationToken: cancellationToken);
        return response.Value.Content[0].Text;
    }
}
