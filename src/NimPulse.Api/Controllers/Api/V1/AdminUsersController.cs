using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NimPulse.Core.Auth;
using NimPulse.Core.Health;
using NimPulse.Core.Users;

namespace NimPulse.Api.Controllers.Api.V1;

/// <summary>Admin-only Benutzerverwaltung — Familienmitglieder direkt anlegen, statt auf Selbstregistrierung angewiesen zu sein.</summary>
[ApiController]
[Route("api/v1/admin/users")]
[Authorize(Roles = "Admin")]
public class AdminUsersController(NimPulseDbContext db) : ControllerBase
{
    private static readonly PasswordHasher<User> PasswordHasher = new();

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var users = await db.Users
            .OrderBy(u => u.CreatedAt)
            .Select(u => new UserSummary(u.Id, u.Email, u.DisplayName, u.Role.ToString(), u.CreatedAt))
            .ToListAsync(cancellationToken);

        return Ok(users);
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            return BadRequest("Ungültige E-Mail-Adresse.");
        }

        if (await db.Users.AnyAsync(u => u.Email == email, cancellationToken))
        {
            return Conflict("E-Mail-Adresse ist bereits registriert.");
        }

        var role = request.IsAdmin ? UserRole.Admin : UserRole.Member;
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? email : request.DisplayName.Trim(),
            Role = role,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        user.PasswordHash = PasswordHasher.HashPassword(user, request.InitialPassword);

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        return Ok(new UserSummary(user.Id, user.Email, user.DisplayName, user.Role.ToString(), user.CreatedAt));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteUser(Guid id, CancellationToken cancellationToken)
    {
        if (id == HttpContext.User.RequireUserId())
        {
            return BadRequest("Der eigene Account kann hier nicht gelöscht werden.");
        }

        var user = await db.Users.FindAsync([id], cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        db.Users.Remove(user);
        await db.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}

public record CreateUserRequest(string Email, string InitialPassword, string DisplayName, bool IsAdmin);

public record UserSummary(Guid Id, string Email, string DisplayName, string Role, DateTimeOffset CreatedAt);
