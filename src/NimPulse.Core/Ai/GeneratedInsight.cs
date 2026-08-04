namespace NimPulse.Core.Ai;

/// <summary>
/// Dedup-Marker für <see cref="WeeklyInsightService"/> — verhindert, dass derselbe Nutzer in
/// derselben ISO-Woche (Montag-Start) mehrfach eine Wochenzusammenfassung bekommt, auch wenn der
/// Hintergrund-Dienst mehrmals pro Woche tickt oder neu startet.
/// </summary>
public class GeneratedInsight
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateOnly WeekStart { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
