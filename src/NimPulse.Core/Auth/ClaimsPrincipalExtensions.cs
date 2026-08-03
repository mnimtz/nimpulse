using System.Security.Claims;

namespace NimPulse.Core.Auth;

public static class ClaimsPrincipalExtensions
{
    /// <summary>Reads the authenticated user's Id from the JWT claims set by <see cref="JwtTokenService"/>.</summary>
    public static Guid RequireUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (value is null || !Guid.TryParse(value, out var userId))
        {
            throw new InvalidOperationException("Kein gültiger UserId-Claim im Token — Endpoint braucht [Authorize].");
        }

        return userId;
    }
}
