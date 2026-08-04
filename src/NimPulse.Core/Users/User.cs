namespace NimPulse.Core.Users;

public class User
{
    public Guid Id { get; set; }

    public string Email { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public string PasswordHash { get; set; } = "";

    public UserRole Role { get; set; } = UserRole.Member;

    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// How far back the iOS app syncs HealthKit data, in days. Null means "alles" (no lower
    /// bound) — but only when the user explicitly chose that; the default is 30, not null, so an
    /// unset preference never silently becomes an unbounded (years-long) first sync.
    /// </summary>
    public int? SyncWindowDays { get; set; } = 30;
}

public enum UserRole
{
    Member,
    Admin,
}
