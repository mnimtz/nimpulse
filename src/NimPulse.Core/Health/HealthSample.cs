namespace NimPulse.Core.Health;

/// <summary>
/// One HealthKit sample (quantity or category), synced from the iOS app.
/// No UserId yet — there's no auth (Phase 2). Every sample belongs to the single
/// implicit local user until multi-user accounts exist.
/// </summary>
public class HealthSample
{
    public Guid Id { get; set; }

    /// <summary>The HealthKit sample UUID — unique per sample, used to upsert instead of duplicating on re-sync.</summary>
    public string ExternalId { get; set; } = "";

    /// <summary>HealthKit identifier, e.g. "stepCount", "sleepAnalysis".</summary>
    public string Type { get; set; } = "";

    public HealthSampleKind Kind { get; set; }

    /// <summary>Set for Kind == Quantity.</summary>
    public double? Value { get; set; }

    /// <summary>Set for Kind == Quantity, e.g. "count", "kg", "count/min".</summary>
    public string? Unit { get; set; }

    /// <summary>Set for Kind == Category, e.g. HKCategoryValueSleepAnalysis raw value.</summary>
    public int? CategoryValue { get; set; }

    public DateTimeOffset StartDate { get; set; }

    public DateTimeOffset EndDate { get; set; }

    /// <summary>Device/app that recorded the sample in Health, e.g. "iPhone", "Apple Watch".</summary>
    public string? SourceName { get; set; }

    public DateTimeOffset SyncedAt { get; set; }
}

public enum HealthSampleKind
{
    Quantity,
    Category,
}
