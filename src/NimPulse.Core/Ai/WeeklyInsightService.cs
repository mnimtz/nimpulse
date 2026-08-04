using Microsoft.EntityFrameworkCore;
using NimPulse.Core.Health;

namespace NimPulse.Core.Ai;

/// <summary>
/// Erzeugt bei Bedarf eine proaktive Wochenzusammenfassung pro Nutzer und legt sie als
/// Assistenten-Nachricht in der bestehenden Chat-Historie ab (siehe <see cref="StoredChatMessage"/>)
/// — kein neuer Zustellweg nötig, sie erscheint einfach beim nächsten Öffnen von /coach bzw. dem
/// iOS-Chat. Dedup über <see cref="GeneratedInsight"/> (eine Zusammenfassung pro Nutzer und
/// ISO-Woche).
/// </summary>
public class WeeklyInsightService(ChatCoachService chatCoachService, AiProviderResolver providerResolver, NimPulseDbContext db)
{
    private const string SummaryPrompt =
        "Du bist der Gesundheits-Assistent von NimPulse. Schreibe einen kurzen, freundlichen " +
        "Wochenrückblick über die Gesundheitsdaten des Nutzers unten. Beginne mit der Kopfzeile " +
        "\"**Dein Wochenrückblick:**\", hebe danach in 2-3 Sätzen ein bis zwei auffällige Trends " +
        "hervor. Keine Diagnosen, keine medizinische Beratung.";

    public async Task GenerateForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var weekStart = CurrentWeekStart();

        var alreadyGenerated = await db.GeneratedInsights
            .AnyAsync(i => i.UserId == userId && i.WeekStart == weekStart, cancellationToken);
        if (alreadyGenerated)
        {
            return;
        }

        try
        {
            var healthContext = await chatCoachService.BuildHealthContextAsync(userId, cancellationToken);
            var provider = await providerResolver.ResolveAsync(null, cancellationToken);
            var summary = await provider.AskAsync(SummaryPrompt, [], healthContext, cancellationToken);

            db.ChatMessages.Add(new StoredChatMessage
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Role = ChatMessageRole.Assistant,
                Content = summary,
                CreatedAt = DateTimeOffset.UtcNow,
            });
            db.GeneratedInsights.Add(new GeneratedInsight
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                WeekStart = weekStart,
                CreatedAt = DateTimeOffset.UtcNow,
            });
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception)
        {
            // Gleiche Boundary-Begründung wie ChatCoachService.SendMessageAsync — ein KI-Provider-
            // Fehler (fehlender Key, Netzwerk, ...) darf den Hintergrund-Dienst nicht abwürgen.
            // Kein GeneratedInsight-Eintrag bei Fehler, damit der nächste Tick es erneut versucht.
        }
    }

    private static DateOnly CurrentWeekStart()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var daysSinceMonday = ((int)today.DayOfWeek + 6) % 7;
        return today.AddDays(-daysSinceMonday);
    }
}
