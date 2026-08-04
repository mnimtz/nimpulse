using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NimPulse.Core.Ai;
using NimPulse.Core.Auth;

namespace NimPulse.Api.Controllers.Api.V1;

[ApiController]
[Route("api/v1/ai")]
[Authorize]
public class AiController(AiProviderResolver providerResolver, ChatCoachService chatCoachService) : ControllerBase
{
    [HttpGet("providers")]
    public IActionResult ListProviders() => Ok(providerResolver.AvailableProviders);

    [HttpGet("chat/history")]
    public async Task<IActionResult> GetHistory(CancellationToken cancellationToken)
    {
        var userId = HttpContext.User.RequireUserId();
        var messages = await chatCoachService.GetHistoryAsync(userId, cancellationToken);
        return Ok(messages.Select(ToView));
    }

    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest("message darf nicht leer sein.");
        }

        var userId = HttpContext.User.RequireUserId();
        var result = await chatCoachService.SendMessageAsync(userId, request.Message, request.Provider, cancellationToken);

        if (result.Error is not null)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(new ChatResponse(result.AssistantMessage!.Content, result.AssistantMessage.CreatedAt));
    }

    private static ChatMessageView ToView(StoredChatMessage message) => new(
        message.Role == ChatMessageRole.User ? "user" : "assistant",
        message.Content,
        message.CreatedAt);
}

public record ChatRequest(string Message, string? Provider);

public record ChatResponse(string Answer, DateTimeOffset CreatedAt);

public record ChatMessageView(string Role, string Content, DateTimeOffset CreatedAt);
