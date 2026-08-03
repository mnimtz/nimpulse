namespace NimPulse.Core.Users;

public class User
{
    public Guid Id { get; set; }

    public string Email { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public string PasswordHash { get; set; } = "";

    public UserRole Role { get; set; } = UserRole.Member;

    public DateTimeOffset CreatedAt { get; set; }
}

public enum UserRole
{
    Member,
    Admin,
}
