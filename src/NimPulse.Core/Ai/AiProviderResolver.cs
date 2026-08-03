using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NimPulse.Core.Health;

namespace NimPulse.Core.Ai;

/// <summary>
/// Picks a provider by name ("claude" / "azure-openai"), or falls back to the admin-configured
/// default (KI-Gateway, DB-backed) — or <see cref="AiOptions.DefaultProvider"/> from appsettings
/// if no admin override exists yet. Scoped (not singleton) because it reads the DbContext.
/// </summary>
public class AiProviderResolver(IEnumerable<IAiProvider> providers, IOptions<AiOptions> options, NimPulseDbContext db)
{
    private readonly Dictionary<string, IAiProvider> _providers =
        providers.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

    public async Task<IAiProvider> ResolveAsync(string? providerName, CancellationToken cancellationToken)
    {
        var name = providerName ?? await GetDefaultProviderNameAsync(cancellationToken);

        return _providers.TryGetValue(name, out var provider)
            ? provider
            : throw new ArgumentException($"Unknown AI provider '{name}'. Available: {string.Join(", ", _providers.Keys)}");
    }

    public async Task<string> GetDefaultProviderNameAsync(CancellationToken cancellationToken)
    {
        var settings = await db.AiGatewaySettings.FindAsync([1], cancellationToken);
        return settings?.DefaultProvider ?? options.Value.DefaultProvider;
    }

    public IReadOnlyCollection<string> AvailableProviders => _providers.Keys;
}
