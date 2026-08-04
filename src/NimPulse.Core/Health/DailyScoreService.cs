namespace NimPulse.Core.Health;

public record ScoreComponent(string Type, double? Value, double Goal, double Weight, double? Contribution);

public record DailyScoreResult(DateTimeOffset Date, double? Score, List<ScoreComponent> Components);

/// <summary>
/// v1 composite daily score — a transparent starting point, not a medical assessment. Weights
/// steps/active energy/resting heart rate; if a metric has no samples on the day, its weight is
/// redistributed proportionally across the remaining available metrics. With no data at all,
/// Score is null rather than a made-up number.
/// </summary>
public class DailyScoreService(ReportService reports)
{
    private const string StepCountType = "stepCount";
    private const string ActiveEnergyType = "activeEnergyBurned";
    private const string RestingHeartRateType = "restingHeartRate";

    private const double StepGoal = 10_000;
    private const double ActiveEnergyGoal = 500;

    public async Task<DailyScoreResult> GetScoreAsync(Guid userId, DateTimeOffset date, CancellationToken cancellationToken = default)
    {
        var stepValue = await GetDayValueAsync(userId, StepCountType, date, ReportBucketValue.Sum, cancellationToken);
        var energyValue = await GetDayValueAsync(userId, ActiveEnergyType, date, ReportBucketValue.Sum, cancellationToken);
        var restingHeartRateValue = await GetDayValueAsync(userId, RestingHeartRateType, date, ReportBucketValue.Average, cancellationToken);

        var components = new List<ScoreComponent>
        {
            new(StepCountType, stepValue, StepGoal, Weight: 0.4, Contribution: ScoreFromGoal(stepValue, StepGoal)),
            new(ActiveEnergyType, energyValue, ActiveEnergyGoal, Weight: 0.3, Contribution: ScoreFromGoal(energyValue, ActiveEnergyGoal)),
            new(RestingHeartRateType, restingHeartRateValue, Goal: 60, Weight: 0.3, Contribution: ScoreFromRestingHeartRate(restingHeartRateValue)),
        };

        var available = components.Where(c => c.Contribution is not null).ToList();
        double? score = available.Count == 0
            ? null
            : available.Sum(c => c.Contribution!.Value * c.Weight) / available.Sum(c => c.Weight);

        return new DailyScoreResult(StartOfDay(date), score, components);
    }

    private async Task<double?> GetDayValueAsync(Guid userId, string type, DateTimeOffset date, ReportBucketValue valueKind, CancellationToken cancellationToken)
    {
        var buckets = await reports.GetBucketsForDateAsync(userId, type, ReportPeriod.Day, date, daysBack: 1, cancellationToken);

        // Samples are stored with whatever offset they arrived in (real syncs are UTC, see
        // HealthController.UploadSamples), while `date` carries the caller's local offset — a
        // raw DateTimeOffset equality check would compare "local midnight" against "UTC midnight",
        // two different instants whenever the offsets differ. Compare local calendar dates instead.
        var wantedLocalDate = date.ToLocalTime().Date;
        var bucket = buckets.FirstOrDefault(b => b.BucketStart.ToLocalTime().Date == wantedLocalDate);
        if (bucket is null)
        {
            return null;
        }

        return valueKind == ReportBucketValue.Sum ? bucket.Sum : bucket.Average;
    }

    private static double? ScoreFromGoal(double? value, double goal)
        => value is null ? null : Math.Min(value.Value / goal, 1) * 100;

    private static double? ScoreFromRestingHeartRate(double? bpm)
    {
        if (bpm is null)
        {
            return null;
        }

        if (bpm <= 60)
        {
            return 100;
        }

        if (bpm >= 100)
        {
            return 50;
        }

        // Linear taper from 100 points at 60 bpm down to 50 points at 100 bpm.
        return 100 - (bpm.Value - 60) / (100 - 60) * 50;
    }

    private static DateTimeOffset StartOfDay(DateTimeOffset date) => ReportService.BucketStart(date, ReportPeriod.Day);

    private enum ReportBucketValue { Sum, Average }
}
