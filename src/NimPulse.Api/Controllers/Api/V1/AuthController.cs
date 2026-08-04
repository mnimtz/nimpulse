using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NimPulse.Core.Auth;
using NimPulse.Core.Health;
using NimPulse.Core.Users;

namespace NimPulse.Api.Controllers.Api.V1;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(NimPulseDbContext db, JwtTokenService tokenService) : ControllerBase
{
    private static readonly PasswordHasher<User> PasswordHasher = new();

    /// <summary>
    /// Offene Selbstregistrierung. Der allererste Account im System wird automatisch Admin —
    /// alle danach sind normale Mitglieder. Passt zum Familien-Setup: wer die App zuerst
    /// einrichtet, verwaltet sie; kein separater Invite-Flow nötig, um loszulegen.
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            return BadRequest("Ungültige E-Mail-Adresse.");
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
        {
            return BadRequest("Passwort muss mindestens 8 Zeichen haben.");
        }

        if (await db.Users.AnyAsync(u => u.Email == email, cancellationToken))
        {
            return Conflict("E-Mail-Adresse ist bereits registriert.");
        }

        var isFirstUser = !await db.Users.AnyAsync(cancellationToken);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? email : request.DisplayName.Trim(),
            Role = isFirstUser ? UserRole.Admin : UserRole.Member,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        user.PasswordHash = PasswordHasher.HashPassword(user, request.Password);

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        return Ok(ToAuthResponse(user));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user is null)
        {
            return Unauthorized("E-Mail oder Passwort ist falsch.");
        }

        var result = PasswordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            return Unauthorized("E-Mail oder Passwort ist falsch.");
        }

        return Ok(ToAuthResponse(user));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var userId = HttpContext.User.RequireUserId();
        var user = await db.Users.FindAsync([userId], cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        return Ok(ToAuthResponse(user, includeToken: false));
    }

    /// <summary>Persönliche Präferenzen (aktuell nur Sync-Zeitraum) — jeder Nutzer setzt seine eigenen, kein Admin-only.</summary>
    [HttpPut("me/preferences")]
    [Authorize]
    public async Task<IActionResult> UpdatePreferences([FromBody] UpdatePreferencesRequest request, CancellationToken cancellationToken)
    {
        var userId = HttpContext.User.RequireUserId();
        var user = await db.Users.FindAsync([userId], cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        user.SyncWindowDays = request.SyncWindowDays;
        await db.SaveChangesAsync(cancellationToken);

        return Ok(ToAuthResponse(user, includeToken: false));
    }

    private AuthResponse ToAuthResponse(User user, bool includeToken = true) => new(
        Token: includeToken ? tokenService.IssueToken(user) : null,
        Id: user.Id,
        Email: user.Email,
        DisplayName: user.DisplayName,
        Role: user.Role.ToString(),
        SyncWindowDays: user.SyncWindowDays);
}

public record RegisterRequest(string Email, string Password, string DisplayName);

public record LoginRequest(string Email, string Password);

public record UpdatePreferencesRequest(int? SyncWindowDays);

public record AuthResponse(string? Token, Guid Id, string Email, string DisplayName, string Role, int? SyncWindowDays);
