using System.ClientModel;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Options;
using NimPulse.Core.Health;
using OpenAI.Chat;

namespace NimPulse.Core.Ai;

/// <summary>
/// Second-opinion provider — same Azure subscription as everything else, no new cloud vendor.
/// Scoped, endpoint/key/deployment resolved lazily inside <see cref="AskAsync"/> (from the
/// DB-backed KI-Gateway settings, falling back to appsettings/env) — constructing the
/// <see cref="AzureOpenAIClient"/> eagerly in the constructor would throw on an empty endpoint
/// even when nobody asked for this provider (e.g. Claude-only setups).
/// </summary>
public class AzureOpenAiProvider(IOptions<AiOptions> options, NimPulseDbContext db) : IAiProvider
{
    public string Name => "azure-openai";

    public async Task<string> AskAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken = default)
    {
        var settings = await db.AiGatewaySettings.FindAsync([1], cancellationToken);
        var fallback = options.Value.AzureOpenAi;
        var endpoint = string.IsNullOrWhiteSpace(settings?.AzureOpenAiEndpoint) ? fallback.Endpoint : settings.AzureOpenAiEndpoint;
        var apiKey = string.IsNullOrWhiteSpace(settings?.AzureOpenAiApiKey) ? fallback.ApiKey : settings.AzureOpenAiApiKey;
        var deploymentName = string.IsNullOrWhiteSpace(settings?.AzureOpenAiDeploymentName) ? fallback.DeploymentName : settings.AzureOpenAiDeploymentName;

        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(deploymentName))
        {
            throw new InvalidOperationException("Azure OpenAI ist nicht konfiguriert — Endpoint, API-Key und Deployment-Name im KI-Gateway (Einstellungen) setzen.");
        }

        var client = new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(apiKey));
        var chatClient = client.GetChatClient(deploymentName);

        ChatMessage[] messages =
        [
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(userMessage),
        ];

        var response = await chatClient.CompleteChatAsync(messages, cancellationToken: cancellationToken);
        return response.Value.Content[0].Text;
    }
}
