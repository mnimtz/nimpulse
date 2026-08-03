namespace NimPulse.Core.Ai;

public class AiOptions
{
    public const string SectionName = "Ai";

    /// <summary>Which provider answers when the caller doesn't pick one explicitly. "claude" or "azure-openai".</summary>
    public string DefaultProvider { get; set; } = "claude";

    public ClaudeOptions Claude { get; set; } = new();

    public AzureOpenAiOptions AzureOpenAi { get; set; } = new();
}

public class ClaudeOptions
{
    public string ApiKey { get; set; } = "";

    /// <summary>Sonnet 5 by default — Opus 5 is available for the deeper monthly-report analysis (Phase 3+).</summary>
    public string Model { get; set; } = "claude-sonnet-5";
}

public class AzureOpenAiOptions
{
    public string Endpoint { get; set; } = "";

    public string ApiKey { get; set; } = "";

    /// <summary>The Azure OpenAI deployment name (configured in the Azure portal), not a raw OpenAI model ID.</summary>
    public string DeploymentName { get; set; } = "";
}
