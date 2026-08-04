using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NimPulse.Core.Auth;
using NimPulse.Core.Health;

namespace NimPulse.Api.Controllers.Api.V1;

[ApiController]
[Route("api/v1/health")]
[Authorize]
public class HealthController(NimPulseDbContext db, ReportService reports, DailyScoreService dailyScore) : ControllerBase
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

        // Same SQLite/EF Core translation limitation as ReportService: a >= comparison on
        // StartDate can't be combined with another predicate server-side. Only the UserId
        // equality runs in SQL; date/type filtering and ordering happen client-side.
        var candidates = await db.HealthSamples
            .Where(s => s.UserId == userId)
            .ToListAsync(cancellationToken);

        var samples = candidates
            .Where(s => s.StartDate >= since && (string.IsNullOrWhiteSpace(type) || s.Type == type))
            .OrderByDescending(s => s.StartDate)
            .Take(1000)
            .ToList();

        return Ok(samples);
    }

    [HttpGet("samples/summary")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var userId = HttpContext.User.RequireUserId();
        var summary = await reports.GetTypeSummaryAsync(userId, cancellationToken);
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
        [FromQuery] DateOnly? date = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            return BadRequest("type ist erforderlich.");
        }

        var userId = HttpContext.User.RequireUserId();

        // `date` verankert das Fenster auf einen bestimmten Tag statt "die letzten N Tage ab jetzt"
        // — für Tag-für-Tag-Navigation (Dashboard) genutzt; ohne `date` unverändertes Verhalten.
        var buckets = date is null
            ? await reports.GetBucketsAsync(userId, type, period, days, cancellationToken)
            : await reports.GetBucketsForDateAsync(userId, type, period, date.Value.ToDateTime(TimeOnly.MinValue), days, cancellationToken);

        return Ok(new Report(type, period.ToString(), buckets));
    }

    /// <summary>
    /// v1-Tages-Score (Schritte/aktive Energie/Ruhepuls, gewichtet) — Startpunkt, keine
    /// medizinische Bewertung. Siehe <see cref="DailyScoreService"/>.
    /// </summary>
    [HttpGet("score")]
    public async Task<IActionResult> GetScore([FromQuery] DateOnly? date, CancellationToken cancellationToken = default)
    {
        var userId = HttpContext.User.RequireUserId();
        var anchor = date is null
            ? DateTimeOffset.Now
            : new DateTimeOffset(date.Value.ToDateTime(TimeOnly.MinValue));

        var result = await dailyScore.GetScoreAsync(userId, anchor, cancellationToken);
        return Ok(result);
    }
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

public record Report(string Type, string Period, List<ReportBucket> Buckets);
