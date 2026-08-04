using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace NimPulse.Core.Ai;

public record AiModelOption(string Id);

/// <summary>
/// Live-Abruf verfügbarer Modelle direkt bei den Provider-APIs, mit dem gerade eingetippten
/// (noch nicht gespeicherten) API-Key — spart das blinde Eintippen einer Modell-ID im KI-Gateway.
/// Reine REST-Aufrufe statt SDK-Client, damit dieselbe Logik unabhängig von SDK-Versionsständen
/// der drei Provider bleibt.
/// </summary>
public class AiModelListingService(IHttpClientFactory httpClientFactory)
{
    public Task<List<AiModelOption>> ListModelsAsync(string provider, string apiKey, string? endpoint, CancellationToken cancellationToken) =>
        provider switch
        {
            "claude" => ListClaudeModelsAsync(apiKey, cancellationToken),
            "openai" => ListOpenAiModelsAsync(apiKey, cancellationToken),
            "azure-openai" => ListAzureOpenAiDeploymentsAsync(endpoint, apiKey, cancellationToken),
            _ => throw new ArgumentException($"Unbekannter Provider: {provider}"),
        };

    private async Task<List<AiModelOption>> ListClaudeModelsAsync(string apiKey, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.anthropic.com/v1/models");
        request.Headers.Add("x-api-key", apiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");

        var response = await client.SendAsync(request, cancellationToken);
        await EnsureSuccess(response, "Claude");

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        return json.GetProperty("data")
            .EnumerateArray()
            .Select(item => new AiModelOption(item.GetProperty("id").GetString()!))
            .ToList();
    }

    private async Task<List<AiModelOption>> ListOpenAiModelsAsync(string apiKey, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.openai.com/v1/models");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var response = await client.SendAsync(request, cancellationToken);
        await EnsureSuccess(response, "OpenAI");

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        return json.GetProperty("data")
            .EnumerateArray()
            .Select(item => item.GetProperty("id").GetString()!)
            // Nur Chat-relevante Modelle — die Liste enthält auch Embeddings/Whisper/TTS/DALL-E/
            // Moderation, die für den KI-Assistenten hier nicht sinnvoll auswählbar sein sollen.
            .Where(id => id.StartsWith("gpt") || id.StartsWith("o1") || id.StartsWith("o3") || id.StartsWith("o4"))
            .OrderDescending()
            .Select(id => new AiModelOption(id))
            .ToList();
    }

    private async Task<List<AiModelOption>> ListAzureOpenAiDeploymentsAsync(string? endpoint, string apiKey, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            throw new ArgumentException("Azure-OpenAI-Endpoint wird für die Deployment-Liste benötigt.");
        }

        // SSRF-Schutz: nur echte Azure-OpenAI-Hosts über HTTPS zulassen. Ohne diese Prüfung könnte
        // ein (Admin-)Aufrufer den Server dazu bringen, interne Adressen (z. B. das Cloud-Metadata-
        // Endpoint 169.254.169.254 oder Intranet-Dienste) mit dem API-Key im Header anzufragen.
        if (!IsAllowedAzureEndpoint(endpoint))
        {
            throw new ArgumentException("Azure-OpenAI-Endpoint muss eine HTTPS-URL auf *.openai.azure.com oder *.cognitiveservices.azure.com sein.");
        }

        // Azure OpenAI erfordert vorab benannte Deployments statt roher Modell-IDs — die Liste
        // zeigt deshalb Deployment-Namen (genau das, was in "Azure-OpenAI-Deployment-Name" gehört).
        var client = httpClientFactory.CreateClient();
        var uri = $"{endpoint.TrimEnd('/')}/openai/deployments?api-version=2024-10-21";
        var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Add("api-key", apiKey);

        var response = await client.SendAsync(request, cancellationToken);
        await EnsureSuccess(response, "Azure OpenAI");

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        return json.GetProperty("data")
            .EnumerateArray()
            .Select(item => new AiModelOption(item.GetProperty("id").GetString()!))
            .ToList();
    }

    private static readonly string[] AllowedAzureHostSuffixes =
    {
        ".openai.azure.com",
        ".cognitiveservices.azure.com",
    };

    private static bool IsAllowedAzureEndpoint(string endpoint)
    {
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (uri.Scheme != Uri.UriSchemeHttps)
        {
            return false;
        }

        var host = uri.Host;
        return AllowedAzureHostSuffixes.Any(suffix => host.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
    }

    private static async Task EnsureSuccess(HttpResponseMessage response, string providerLabel)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        // Deliberately do NOT reflect the upstream response body back to the caller — for an
        // attacker-influenced endpoint that body could carry internal/SSRF response content.
        await response.Content.ReadAsStringAsync();
        throw new InvalidOperationException($"{providerLabel}-API antwortete mit {(int)response.StatusCode}.");
    }
}
