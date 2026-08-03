using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NimPulse.Core.Auth;
using NimPulse.Core.Health;

namespace NimPulse.Api.Controllers.Api.V1;

[ApiController]
[Route("api/v1/health")]
[Authorize]
public class HealthController(NimPulseDbContext db) : ControllerBase
{
    /// <summary>
    /// Upserts a batch of HealthKit samples for the authenticated user by ExternalId, so
    /// re-syncing the same time range never duplicates rows.
    /// </summary>
    [HttpPost("samples")]
    public async Task<IActionResult> UploadSamples([FromBody] UploadSamplesRequest request, CancellationToken cancellationToken)
    {
        if (request.Samples.Count == 0)
        {
            return Ok(new UploadSamplesResponse(Received: 0, Inserted: 0, Updated: 0));
        }

        var userId = HttpContext.User.RequireUserId();

        var externalIds = request.Samples.Select(s => s.ExternalId).ToList();
        var existing = await db.HealthSamples
            .Where(s => s.UserId == userId && externalIds.Contains(s.ExternalId))
            .ToDictionaryAsync(s => s.ExternalId, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var inserted = 0;
        var updated = 0;

        foreach (var sample in request.Samples)
        {
            var startDate = DateTimeOffset.FromUnixTimeMilliseconds(sample.StartDateUnixMs);
            var endDate = DateTimeOffset.FromUnixTimeMilliseconds(sample.EndDateUnixMs);

            if (existing.TryGetValue(sample.ExternalId, out var row))
            {
                row.Value = sample.Value;
                row.Unit = sample.Unit;
                row.CategoryValue = sample.CategoryValue;
                row.StartDate = startDate;
                row.EndDate = endDate;
                row.SourceName = sample.SourceName;
                row.SyncedAt = now;
                updated++;
            }
            else
            {
                db.HealthSamples.Add(new HealthSample
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    ExternalId = sample.ExternalId,
                    Type = sample.Type,
                    Kind = sample.Kind,
                    Value = sample.Value,
                    Unit = sample.Unit,
                    CategoryValue = sample.CategoryValue,
                    StartDate = startDate,
                    EndDate = endDate,
                    SourceName = sample.SourceName,
                    SyncedAt = now,
                });
                inserted++;
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        return Ok(new UploadSamplesResponse(request.Samples.Count, inserted, updated));
    }

    [HttpGet("samples")]
    public async Task<IActionResult> GetSamples(
        [FromQuery] string? type,
        [FromQuery] int days = 7,
        CancellationToken cancellationToken = default)
    {
        var userId = HttpContext.User.RequireUserId();
        var since = DateTimeOffset.UtcNow.AddDays(-Math.Abs(days));

        var query = db.HealthSamples.Where(s => s.UserId == userId && s.StartDate >= since);
        if (!string.IsNullOrWhiteSpace(type))
        {
            query = query.Where(s => s.Type == type);
        }

        var samples = await query
            .OrderByDescending(s => s.StartDate)
            .Take(1000)
            .ToListAsync(cancellationToken);

        return Ok(samples);
    }

    [HttpGet("samples/summary")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var userId = HttpContext.User.RequireUserId();

        var summary = await db.HealthSamples
            .Where(s => s.UserId == userId)
            .GroupBy(s => s.Type)
            .Select(g => new TypeSummary(g.Key, g.Count(), g.Max(s => s.StartDate)))
            .OrderByDescending(s => s.LatestSampleAt)
            .ToListAsync(cancellationToken);

        return Ok(summary);
    }

    /// <summary>
    /// Aggregiert Quantity-Samples eines Typs über einen Zeitraum in Buckets (Tag/Woche/Monat).
    /// Die Basis für "verschiedene Reports" — Wochenübersicht, Monatstrend etc. sind alle derselbe
    /// Aufruf mit anderem `period`/`days`; ein PDF-Export darauf ist ein späterer Schritt (Phase 3).
    /// </summary>
    [HttpGet("reports")]
    public async Task<IActionResult> GetReport(
        [FromQuery] string type,
        [FromQuery] ReportPeriod period = ReportPeriod.Day,
        [FromQuery] int days = 30,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            return BadRequest("type ist erforderlich.");
        }

        var userId = HttpContext.User.RequireUserId();
        var since = DateTimeOffset.UtcNow.AddDays(-Math.Abs(days));

        var samples = await db.HealthSamples
            .Where(s => s.UserId == userId && s.Type == type && s.StartDate >= since && s.Value != null)
            .Select(s => new { s.StartDate, s.Value })
            .ToListAsync(cancellationToken);

        var buckets = samples
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

        return Ok(new Report(type, period.ToString(), buckets));
    }

    private static DateTimeOffset BucketStart(DateTimeOffset date, ReportPeriod period)
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

public enum ReportPeriod
{
    Day,
    Week,
    Month,
}

public record UploadSamplesRequest(List<HealthSampleDto> Samples);

public record HealthSampleDto(
    string ExternalId,
    string Type,
    HealthSampleKind Kind,
    double? Value,
    string? Unit,
    int? CategoryValue,
    // Unix-Millisekunden statt DateTimeOffset/ISO-8601-String — .NETs Standard-Rundlaufformat
    // ("...0000000+00:00") und Swifts Codable-.iso8601-Strategie ("...Z", keine
    // Nachkommastellen) sind nicht kompatibel; ein Long ist auf beiden Seiten eindeutig.
    long StartDateUnixMs,
    long EndDateUnixMs,
    string? SourceName);

public record UploadSamplesResponse(int Received, int Inserted, int Updated);

public record TypeSummary(string Type, int Count, DateTimeOffset LatestSampleAt);

public record ReportBucket(DateTimeOffset BucketStart, int Count, double Sum, double Average, double Min, double Max);

public record Report(string Type, string Period, List<ReportBucket> Buckets);
