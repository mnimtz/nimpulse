namespace NimPulse.Core.Auth;

public class AuthOptions
{
    public const string SectionName = "Auth";

    /// <summary>
    /// Symmetric signing key for JWTs. Must be set via config/env in any real deployment —
    /// the fallback below is only so local dev doesn't crash on a missing setting, and is
    /// deliberately obvious/unusable in production (short, well-known string).
    /// </summary>
    public string JwtSigningKey { get; set; } = "dev-only-insecure-signing-key-change-me";

    public string Issuer { get; set; } = "NimPulse";

    public string Audience { get; set; } = "NimPulse";

    public int TokenLifetimeHours { get; set; } = 24 * 30;
}
