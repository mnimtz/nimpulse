namespace NimPulse.Core.Auth;

public class AuthOptions
{
    public const string SectionName = "Auth";

    /// <summary>
    /// Well-known, deliberately-insecure development signing key. Used only as a fallback so a
    /// local dev run doesn't crash on a missing setting. A startup guard (see Program.cs) refuses
    /// to boot in any non-Development environment while this value (or a missing/too-short key)
    /// is in effect — real deployments MUST override it via Auth__JwtSigningKey.
    /// </summary>
    public const string InsecureDevSigningKey = "dev-only-insecure-signing-key-change-me";

    /// <summary>
    /// Symmetric signing key for JWTs. Must be set via config/env in any real deployment — the
    /// fallback is only for local dev and is rejected at startup outside Development.
    /// </summary>
    public string JwtSigningKey { get; set; } = InsecureDevSigningKey;

    public string Issuer { get; set; } = "NimPulse";

    public string Audience { get; set; } = "NimPulse";

    public int TokenLifetimeHours { get; set; } = 24 * 30;
}
