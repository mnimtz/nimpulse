using Anthropic;
using Anthropic.Models.Messages;
using Microsoft.Extensions.Options;
using NimPulse.Core.Health;

namespace NimPulse.Core.Ai;

/// <summary>
/// Primary AI provider — chat, weekly insights, everything that should decline rather than
/// diagnose. Scoped (not singleton): the API key/model come from the DB-backed KI-Gateway
/// settings (admin-configurable at runtime), falling back to appsettings/env for local dev.
/// </summary>
public class ClaudeAiProvider(IOptions<AiOptions> options, NimPulseDbContext db) : IAiProvider
{
    public string Name => "claude";

    public async Task<string> AskAsync(string systemPrompt, IReadOnlyList<ChatTurn> history, string userMessage, CancellationToken cancellationToken = default)
    {
        var settings = await db.AiGatewaySettings.FindAsync([1], cancellationToken);
        var apiKey = string.IsNullOrWhiteSpace(settings?.ClaudeApiKey) ? options.Value.Claude.ApiKey : settings.ClaudeApiKey;
        var model = string.IsNullOrWhiteSpace(settings?.ClaudeModel) ? options.Value.Claude.Model : settings.ClaudeModel;

        var messages = history
            .Select(turn => new MessageParam { Role = turn.Role == "user" ? Role.User : Role.Assistant, Content = turn.Content })
            .Append(new MessageParam { Role = Role.User, Content = userMessage })
            .ToList();

        var client = new AnthropicClient { ApiKey = apiKey };
        var response = await client.Messages.Create(new MessageCreateParams
        {
            Model = model,
            MaxTokens = 2048,
            System = systemPrompt,
            Messages = messages,
        });

        return string.Concat(response.Content
            .Select(block => block.Value)
            .OfType<TextBlock>()
            .Select(text => text.Text));
    }
}
