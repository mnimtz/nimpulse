using Microsoft.EntityFrameworkCore;
using NimPulse.Core.Ai;
using NimPulse.Core.Health;

namespace NimPulse.Api.BackgroundServices;

/// <summary>
/// Tickt alle paar Stunden und lässt <see cref="WeeklyInsightService"/> für jeden Nutzer prüfen,
/// ob diese ISO-Woche schon eine Zusammenfassung erzeugt wurde. Läuft in einem eigenen Scope pro
/// Tick (DbContext/ChatCoachService sind scoped, dieser Dienst selbst ist singleton).
/// </summary>
public class WeeklyInsightBackgroundService(IServiceScopeFactory scopeFactory, ILogger<WeeklyInsightBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromHours(6);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "WeeklyInsightBackgroundService-Tick fehlgeschlagen.");
            }

            try
            {
                await Task.Delay(TickInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Shutdown während des Wartens — Schleife beendet sich über die while-Bedingung.
            }
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NimPulseDbContext>();
        var insightService = scope.ServiceProvider.GetRequiredService<WeeklyInsightService>();

        var userIds = await db.Users.Select(u => u.Id).ToListAsync(cancellationToken);

        foreach (var userId in userIds)
        {
            try
            {
                await insightService.GenerateForUserAsync(userId, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Ein fehlerhafter Nutzer darf den Lauf für alle anderen nicht abbrechen.
                logger.LogError(ex, "Wochenzusammenfassung für Nutzer {UserId} fehlgeschlagen.", userId);
            }
        }
    }
}
