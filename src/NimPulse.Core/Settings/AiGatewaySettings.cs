namespace NimPulse.Core.Settings;

/// <summary>
/// Singleton row (Id is always 1) — admin-configurable defaults for the AI gateway.
/// API keys stay in appsettings/env (secrets, not admin-editable via this table); only the
/// routing/default-model choice lives here so an Admin can switch it at runtime.
/// </summary>
public class AiGatewaySettings
{
    public int Id { get; set; } = 1;

    public string DefaultProvider { get; set; } = "claude";

    public string ClaudeModel { get; set; } = "claude-sonnet-5";

    public string AzureOpenAiDeploymentName { get; set; } = "";

    public DateTimeOffset UpdatedAt { get; set; }
}
