using Microsoft.EntityFrameworkCore;
using NimPulse.Core.Health;

namespace NimPulse.Core.Ai;

public record ChatSendResult(StoredChatMessage UserMessage, StoredChatMessage? AssistantMessage, string? Error);

/// <summary>
/// Shared KI-Coach logic — used by both <c>AiController</c> (iOS/API clients) and the web
/// <c>Coach.razor</c> page (runs server-side in the Blazor circuit, calls this directly instead
/// of round-tripping through its own HTTP API).
/// </summary>
public class ChatCoachService(AiProviderResolver providerResolver, ReportService reportService, NimPulseDbContext db)
{
    private const string SystemPromptBase =
        "Du bist der Gesundheits-Assistent von NimPulse. Du erklärst die Gesundheitsdaten des Nutzers " +
        "verständlich und konkret. Du stellst keine Diagnosen, gibst keine medizinische Beratung und " +
        "empfiehlst bei ernsthaften Anliegen, eine Ärztin oder einen Arzt zu konsultieren.";

    // Dieselbe kuratierte Auswahl wie im Web-Dashboard (Home.razor) — nicht jeder synchronisierte
    // Typ gehört in jeden Prompt, das würde den Kontext unnötig aufblähen.
    private static readonly string[] ContextTypes = ["stepCount", "activeEnergyBurned", "heartRate", "restingHeartRate", "bodyMass"];

    public async Task<List<StoredChatMessage>> GetHistoryAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // SQLite/EF Core kann nicht serverseitig nach DateTimeOffset sortieren — erst laden, dann
        // clientseitig sortieren (dieselbe Einschränkung wie in ReportService/AdminUsersController).
        var messages = await db.ChatMessages.Where(m => m.UserId == userId).ToListAsync(cancellationToken);
        return messages.OrderBy(m => m.CreatedAt).ToList();
    }

    public async Task<ChatSendResult> SendMessageAsync(Guid userId, string message, string? providerName, CancellationToken cancellationToken = default)
    {
        var priorMessages = await db.ChatMessages.Where(m => m.UserId == userId).ToListAsync(cancellationToken);
        var history = priorMessages
            .OrderBy(m => m.CreatedAt)
            .Select(m => new ChatTurn(m.Role == ChatMessageRole.User ? "user" : "assistant", m.Content))
            .ToList();

        var userMessage = new StoredChatMessage
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Role = ChatMessageRole.User,
            Content = message,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        db.ChatMessages.Add(userMessage);
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            var systemPrompt = SystemPromptBase + "\n\n" + await BuildHealthContextAsync(userId, cancellationToken);
            var provider = await providerResolver.ResolveAsync(providerName, cancellationToken);
            var answer = await provider.AskAsync(systemPrompt, history, message, cancellationToken);

            var assistantMessage = new StoredChatMessage
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Role = ChatMessageRole.Assistant,
                Content = answer,
                CreatedAt = DateTimeOffset.UtcNow,
            };
            db.ChatMessages.Add(assistantMessage);
            await db.SaveChangesAsync(cancellationToken);

            return new ChatSendResult(userMessage, assistantMessage, null);
        }
        catch (Exception ex)
        {
            // Echte Boundary zu drei verschiedenen externen APIs (Anthropic/OpenAI/Azure OpenAI),
            // jede mit eigener Exception-Hierarchie (bestätigt: AnthropicUnauthorizedException bei
            // fehlendem Key, ArgumentException bei unbekanntem Provider, InvalidOperationException
            // bei "nicht konfiguriert") — ein einzelner Catch-Typ würde reale Fehlerfälle unbehandelt
            // durchlassen. Die Nutzer-Nachricht bleibt trotzdem gespeichert, nur die Antwort fehlt.
            return new ChatSendResult(userMessage, null, ex.Message);
        }
    }

    /// <summary>internal statt private — auch von <see cref="WeeklyInsightService"/> genutzt.</summary>
    internal async Task<string> BuildHealthContextAsync(Guid userId, CancellationToken cancellationToken)
    {
        var summary = await reportService.GetTypeSummaryAsync(userId, cancellationToken);
        if (summary.Count == 0)
        {
            return "Der Nutzer hat noch keine Gesundheitsdaten synchronisiert.";
        }

        var availableTypes = summary.Select(s => s.Type).ToHashSet();
        var lines = new List<string> { "Aktuelle Gesundheitsdaten des Nutzers (letzte 7 Tage, sofern vorhanden):" };

        foreach (var type in ContextTypes)
        {
            if (!availableTypes.Contains(type))
            {
                continue;
            }

            var buckets = await reportService.GetBucketsAsync(userId, type, ReportPeriod.Day, 7, cancellationToken);
            if (buckets.Count == 0)
            {
                continue;
            }

            var label = HealthTypeCatalog.GetDisplayName(type);
            var latest = buckets.OrderByDescending(b => b.BucketStart).First();
            var average = buckets.Average(b => b.Sum);
            lines.Add($"- {label}: zuletzt {latest.Sum:0.#}, Ø letzte 7 Tage {average:0.#}");
        }

        return lines.Count > 1 ? string.Join("\n", lines) : "Der Nutzer hat Gesundheitsdaten synchronisiert, aber keine der üblichen Kernmetriken (Schritte, Energie, Herzfrequenz).";
    }
}
