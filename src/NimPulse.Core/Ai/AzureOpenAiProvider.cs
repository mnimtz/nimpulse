using System.ClientModel;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace NimPulse.Core.Ai;

/// <summary>Second-opinion provider — same Azure subscription as everything else, no new cloud vendor.</summary>
public class AzureOpenAiProvider : IAiProvider
{
    private readonly ChatClient _chatClient;

    public string Name => "azure-openai";

    public AzureOpenAiProvider(IOptions<AiOptions> options)
    {
        var azureOptions = options.Value.AzureOpenAi;
        var client = new AzureOpenAIClient(
            new Uri(azureOptions.Endpoint),
            new ApiKeyCredential(azureOptions.ApiKey));
        _chatClient = client.GetChatClient(azureOptions.DeploymentName);
    }

    public async Task<string> AskAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken = default)
    {
        ChatMessage[] messages =
        [
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(userMessage),
        ];

        var response = await _chatClient.CompleteChatAsync(messages, cancellationToken: cancellationToken);
        return response.Value.Content[0].Text;
    }
}
