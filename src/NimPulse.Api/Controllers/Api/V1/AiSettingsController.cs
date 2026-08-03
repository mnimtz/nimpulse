using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NimPulse.Core.Health;
using NimPulse.Core.Settings;

namespace NimPulse.Api.Controllers.Api.V1;

/// <summary>
/// KI-Gateway: Admin wählt den Standard-Provider/Modell zur Laufzeit, ohne Redeploy. API-Keys
/// bleiben bewusst in appsettings/env (Secrets) — hier steht nur, welcher Provider standardmäßig
/// antwortet.
/// </summary>
[ApiController]
[Route("api/v1/settings/ai")]
[Authorize(Roles = "Admin")]
public class AiSettingsController(NimPulseDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var settings = await db.AiGatewaySettings.FindAsync([1], cancellationToken)
            ?? new AiGatewaySettings();

        return Ok(settings);
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] AiGatewaySettings request, CancellationToken cancellationToken)
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
        settings.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        return Ok(settings);
    }
}
