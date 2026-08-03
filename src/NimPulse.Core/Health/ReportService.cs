using Microsoft.EntityFrameworkCore;

namespace NimPulse.Core.Health;

public enum ReportPeriod
{
    Day,
    Week,
    Month,
}

public record ReportBucket(DateTimeOffset BucketStart, int Count, double Sum, double Average, double Min, double Max);

public record TypeSummary(string Type, int Count, DateTimeOffset LatestSampleAt);

/// <summary>Shared aggregation logic — called from the API (HealthController) and the web dashboard's Reports/Home pages.</summary>
public class ReportService(NimPulseDbContext db)
{
    /// <summary>
    /// Per-type sample count + latest timestamp. GroupBy+Max must run client-side (LINQ to
    /// Objects) — the SQLite EF Core provider can't translate MAX() over DateTimeOffset columns
    /// server-side ("NotSupportedException: SQLite cannot apply aggregate operator 'Max' on
    /// expressions of type 'DateTimeOffset'"), so the Type/StartDate pairs are materialized first.
    /// </summary>
    public async Task<List<TypeSummary>> GetTypeSummaryAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var rows = await db.HealthSamples
            .Where(s => s.UserId == userId)
            .Select(s => new { s.Type, s.StartDate })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(s => s.Type)
            .Select(g => new TypeSummary(g.Key, g.Count(), g.Max(s => s.StartDate)))
            .OrderByDescending(s => s.LatestSampleAt)
            .ToList();
    }

    public async Task<List<ReportBucket>> GetBucketsAsync(Guid userId, string type, ReportPeriod period, int days, CancellationToken cancellationToken = default)
    {
        var since = DateTimeOffset.UtcNow.AddDays(-Math.Abs(days));

        // The SQLite EF Core provider can't translate a >= comparison on StartDate (DateTimeOffset)
        // when combined with any other predicate in the same server-side Where — confirmed via
        // direct testing, not just with Value != null. Only the UserId equality runs server-side;
        // Type/StartDate/Value filtering happens client-side after materializing.
        var samples = await db.HealthSamples
            .Where(s => s.UserId == userId)
            .Select(s => new { s.Type, s.StartDate, s.Value })
            .ToListAsync(cancellationToken);

        return samples
            .Where(s => s.Type == type && s.StartDate >= since && s.Value != null)
            .GroupBy(s => BucketStart(s.StartDate, period))
            .Select(g => new ReportBucket(
                BucketStart: g.Key,
                Count: g.Count(),
                Sum: g.Sum(s => s.Value!.Value),
                Average: g.Average(s => s.Value!.Value),
                Min: g.Min(s => s.Value!.Value),
                Max: g.Max(s => s.Value!.Value)))
            .OrderBy(b => b.BucketStart)
            .ToList();
    }

    public static DateTimeOffset BucketStart(DateTimeOffset date, ReportPeriod period)
    {
        var dayStart = new DateTimeOffset(date.Year, date.Month, date.Day, 0, 0, 0, date.Offset);
        return period switch
        {
            ReportPeriod.Day => dayStart,
            ReportPeriod.Week => dayStart.AddDays(-(int)date.DayOfWeek),
            ReportPeriod.Month => new DateTimeOffset(date.Year, date.Month, 1, 0, 0, 0, date.Offset),
            _ => dayStart,
        };
    }
}
