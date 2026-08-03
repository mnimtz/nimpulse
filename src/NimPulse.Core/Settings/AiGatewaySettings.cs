namespace NimPulse.Core.Settings;

/// <summary>
/// Singleton row (Id is always 1) — admin-configurable AI gateway, including the API keys.
/// Deliberately DB-backed (not appsettings/env-only): the 1-Click-Deploy form shouldn't ask for
/// AI provider secrets at infra-provisioning time — an Admin sets them once, after first login,
/// through the app's own Settings screen. Plaintext in SQLite, same trust boundary as the rest of
/// the app's data on this server — not a Key Vault-grade secret store, consistent with "integrated
/// database, simple" for a personal/family-scale deployment. appsettings/env values (see
/// <see cref="Ai.AiOptions"/>) remain a fallback for local dev only.
/// </summary>
public class AiGatewaySettings
{
    public int Id { get; set; } = 1;

    public string DefaultProvider { get; set; } = "claude";

    public string ClaudeModel { get; set; } = "claude-sonnet-5";

    public string? ClaudeApiKey { get; set; }

    public string AzureOpenAiDeploymentName { get; set; } = "";

    public string? AzureOpenAiEndpoint { get; set; }

    public string? AzureOpenAiApiKey { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
