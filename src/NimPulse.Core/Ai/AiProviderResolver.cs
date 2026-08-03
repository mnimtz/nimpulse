using Microsoft.Extensions.Options;

namespace NimPulse.Core.Ai;

/// <summary>Picks a provider by name ("claude" / "azure-openai"), or falls back to <see cref="AiOptions.DefaultProvider"/>.</summary>
public class AiProviderResolver(IEnumerable<IAiProvider> providers, IOptions<AiOptions> options)
{
    private readonly Dictionary<string, IAiProvider> _providers =
        providers.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

    private readonly string _defaultProvider = options.Value.DefaultProvider;

    public IAiProvider Resolve(string? providerName) =>
        _providers.TryGetValue(providerName ?? _defaultProvider, out var provider)
            ? provider
            : throw new ArgumentException($"Unknown AI provider '{providerName}'. Available: {string.Join(", ", _providers.Keys)}");

    public IReadOnlyCollection<string> AvailableProviders => _providers.Keys;
}
