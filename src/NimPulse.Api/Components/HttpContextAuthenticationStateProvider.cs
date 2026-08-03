using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace NimPulse.Api.Components;

/// <summary>
/// Feeds Blazor's cascading auth state from the current request's HttpContext.User — the web UI
/// is static server rendering only (no interactive circuit), so there's no separate SignalR
/// connection to revalidate; each render is one HTTP request with its own resolved principal
/// (Cookie or Bearer, via the "Smart" policy scheme in Program.cs).
/// </summary>
public class HttpContextAuthenticationStateProvider(IHttpContextAccessor httpContextAccessor) : AuthenticationStateProvider
{
    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var user = httpContextAccessor.HttpContext?.User ?? new ClaimsPrincipal(new ClaimsIdentity());
        return Task.FromResult(new AuthenticationState(user));
    }
}
