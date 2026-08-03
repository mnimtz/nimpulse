using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NimPulse.Core.Ai;

namespace NimPulse.Api.Controllers.Api.V1;

[ApiController]
[Route("api/v1/ai")]
[Authorize]
public class AiController(AiProviderResolver providerResolver) : ControllerBase
{
    private const string SystemPrompt =
        "Du bist der Gesundheits-Assistent von NimPulse. Du erklärst die Gesundheitsdaten des Nutzers " +
        "verständlich und konkret. Du stellst keine Diagnosen, gibst keine medizinische Beratung und " +
        "empfiehlst bei ernsthaften Anliegen, eine Ärztin oder einen Arzt zu konsultieren.";

    [HttpGet("providers")]
    public IActionResult ListProviders() => Ok(providerResolver.AvailableProviders);

    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest("message darf nicht leer sein.");
        }

        try
        {
            var provider = await providerResolver.ResolveAsync(request.Provider, cancellationToken);
            var answer = await provider.AskAsync(SystemPrompt, request.Message, cancellationToken);
            return Ok(new ChatResponse(provider.Name, answer));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}

public record ChatRequest(string Message, string? Provider);

public record ChatResponse(string Provider, string Answer);
