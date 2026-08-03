using Anthropic;
using Anthropic.Models.Messages;
using Microsoft.Extensions.Options;

namespace NimPulse.Core.Ai;

/// <summary>Primary AI provider — chat, weekly insights, everything that should decline rather than diagnose.</summary>
public class ClaudeAiProvider : IAiProvider
{
    private readonly AnthropicClient _client;
    private readonly string _model;

    public string Name => "claude";

    public ClaudeAiProvider(IOptions<AiOptions> options)
    {
        var claudeOptions = options.Value.Claude;
        _client = new AnthropicClient { ApiKey = claudeOptions.ApiKey };
        _model = claudeOptions.Model;
    }

    public async Task<string> AskAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken = default)
    {
        var response = await _client.Messages.Create(new MessageCreateParams
        {
            Model = _model,
            MaxTokens = 2048,
            System = systemPrompt,
            Messages = [new() { Role = Role.User, Content = userMessage }],
        });

        return string.Concat(response.Content
            .Select(block => block.Value)
            .OfType<TextBlock>()
            .Select(text => text.Text));
    }
}
