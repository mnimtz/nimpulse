using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NimPulse.Core.Health;
using NimPulse.Core.Settings;

namespace NimPulse.Api.Controllers.Api.V1;

/// <summary>
/// KI-Gateway: Admin konfiguriert Provider, Modelle und API-Keys zur Laufzeit, ohne Redeploy und
/// ohne dass das 1-Click-Deploy-Formular danach fragen muss. GET maskiert die Keys (nur ob einer
/// gesetzt ist, nie der Wert selbst) — PUT lässt ein leeres Key-Feld unangetastet, damit man nicht
/// bei jeder Änderung alle Keys neu eintippen muss.
/// </summary>
[ApiController]
[Route("api/v1/settings/ai")]
[Authorize(Roles = "Admin")]
public class AiSettingsController(NimPulseDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var settings = await db.AiGatewaySettings.FindAsync([1], cancellationToken) ?? new AiGatewaySettings();
        return Ok(ToView(settings));
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateAiGatewaySettingsRequest request, CancellationToken cancellationToken)
    {
        var settings = await db.AiGatewaySettings.FindAsync([1], cancellationToken);
        if (settings is null)
        {
            settings = new AiGatewaySettings { Id = 1 };
            db.AiGatewaySettings.Add(settings);
        }

        settings.DefaultProvider = request.DefaultProvider;
        settings.ClaudeModel = request.ClaudeModel;
        settings.AzureOpenAiDeploymentName = request.AzureOpenAiDeploymentName;
        settings.AzureOpenAiEndpoint = request.AzureOpenAiEndpoint;

        // Leeres Feld = "unverändert lassen", nicht "Key löschen" — sonst müsste man bei jeder
        // Änderung (z. B. nur das Modell wechseln) alle Keys neu eintippen.
        if (!string.IsNullOrWhiteSpace(request.ClaudeApiKey))
        {
            settings.ClaudeApiKey = request.ClaudeApiKey;
        }
        if (!string.IsNullOrWhiteSpace(request.AzureOpenAiApiKey))
        {
            settings.AzureOpenAiApiKey = request.AzureOpenAiApiKey;
        }

        settings.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return Ok(ToView(settings));
    }

    private static AiGatewaySettingsView ToView(AiGatewaySettings settings) => new(
        DefaultProvider: settings.DefaultProvider,
        ClaudeModel: settings.ClaudeModel,
        HasClaudeApiKey: !string.IsNullOrWhiteSpace(settings.ClaudeApiKey),
        AzureOpenAiDeploymentName: settings.AzureOpenAiDeploymentName,
        AzureOpenAiEndpoint: settings.AzureOpenAiEndpoint,
        HasAzureOpenAiApiKey: !string.IsNullOrWhiteSpace(settings.AzureOpenAiApiKey),
        UpdatedAt: settings.UpdatedAt);
}

public record AiGatewaySettingsView(
    string DefaultProvider,
    string ClaudeModel,
    bool HasClaudeApiKey,
    string AzureOpenAiDeploymentName,
    string? AzureOpenAiEndpoint,
    bool HasAzureOpenAiApiKey,
    DateTimeOffset UpdatedAt);

public record UpdateAiGatewaySettingsRequest(
    string DefaultProvider,
    string ClaudeModel,
    string? ClaudeApiKey,
    string AzureOpenAiDeploymentName,
    string? AzureOpenAiEndpoint,
    string? AzureOpenAiApiKey);
