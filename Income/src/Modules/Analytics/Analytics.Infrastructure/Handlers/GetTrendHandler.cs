using Analytics.Contracts.DTOs;
using Analytics.Contracts.Queries;
using FluentResults;
using Income.Contracts.Queries;

namespace Analytics.Infrastructure.Handlers;

internal sealed class GetTrendHandler(
    IGetAllStreamsHandler streamsHandler) : IGetTrendHandler
{
    public async Task<Result<TrendDto>> HandleAsync(GetTrendQuery query, CancellationToken ct = default)
    {
        var streamsResult = await streamsHandler.HandleAsync(new GetAllStreamsQuery(query.StreamType, query.ProviderId), ct);
        if (streamsResult.IsFailed)
            return streamsResult.ToResult<TrendDto>();

        var streams = streamsResult.Value;

        var filteredSnapshots = streams
            .Where(s => query.StreamId is null || s.Id == query.StreamId)
            .Where(s => query.Category is null || s.Category == query.Category)
            .SelectMany(s => s.Snapshots)
            .ToList();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var periodStart = GetPeriodStart(query.Period, query.PeriodsBack);

        // For fair comparison, filter snapshots to equivalent periods
        // e.g., for monthly: only include days 1-22 of each month if today is the 22nd
        var equivalentSnapshots = FilterToEquivalentPeriods(filteredSnapshots, query.Period, today);

        var groupedByPeriod = GroupByPeriod(equivalentSnapshots, query.Period)
            .Where(g => g.Key >= periodStart)
            .OrderBy(g => g.Key)
            .ToList();

        var points = new List<TrendPointDto>();
        decimal cumulative = 0;
        decimal previousAmount = 0;

        foreach (var group in groupedByPeriod)
        {
            var amount = group.Sum(s => s.UsdAmount);
            cumulative += amount;

            var growthFromPrevious = amount - previousAmount;
            var growthPercentage = previousAmount != 0 ? (growthFromPrevious / previousAmount) * 100 : 0;

            points.Add(new TrendPointDto(
                Date: group.Key,
                AmountUsd: amount,
                CumulativeUsd: cumulative,
                GrowthFromPreviousUsd: points.Count > 0 ? growthFromPrevious : 0,
                GrowthFromPreviousPercentage: points.Count > 0 ? Math.Round(growthPercentage, 2) : 0));

            previousAmount = amount;
        }

        var overallGrowthRate = points.Count >= 2 && points[0].AmountUsd != 0
            ? ((points[^1].AmountUsd - points[0].AmountUsd) / points[0].AmountUsd) * 100
            : 0;

        var avgGrowth = points.Count >= 2
            ? points.Skip(1).Average(p => (double)p.GrowthFromPreviousUsd)
            : 0;

        var direction = overallGrowthRate switch
        {
            > 5 => "Upward",
            < -5 => "Downward",
            _ => "Stable"
        };

        return Result.Ok(new TrendDto(
            Period: query.Period,
            GrowthRatePercentage: Math.Round(overallGrowthRate, 2),
            Direction: direction,
            AverageGrowthPerPeriodUsd: Math.Round((decimal)avgGrowth, 2),
            Points: points));
    }

    /// <summary>
    /// Filters snapshots to equivalent periods for fair comparison.
    /// For example, if today is Jan 22, only includes days 1-22 of each month.
    /// </summary>
    private static List<Income.Contracts.DTOs.SnapshotDto> FilterToEquivalentPeriods(
        List<Income.Contracts.DTOs.SnapshotDto> snapshots,
        string period,
        DateOnly today)
    {
        return period.ToLower() switch
        {
            "monthly" => snapshots.Where(s => s.Date.Day <= today.Day).ToList(),
            "weekly" => snapshots.Where(s => (int)s.Date.DayOfWeek <= (int)today.DayOfWeek).ToList(),
            "quarterly" => FilterQuarterlyEquivalent(snapshots, today),
            "yearly" => snapshots.Where(s => s.Date.DayOfYear <= today.DayOfYear).ToList(),
            _ => snapshots // Daily doesn't need filtering
        };
    }

    private static List<Income.Contracts.DTOs.SnapshotDto> FilterQuarterlyEquivalent(
        List<Income.Contracts.DTOs.SnapshotDto> snapshots,
        DateOnly today)
    {
        var currentQuarterStart = new DateOnly(today.Year, ((today.Month - 1) / 3) * 3 + 1, 1);
        var dayIntoQuarter = today.DayNumber - currentQuarterStart.DayNumber;

        return snapshots.Where(s =>
        {
            var snapshotQuarterStart = new DateOnly(s.Date.Year, ((s.Date.Month - 1) / 3) * 3 + 1, 1);
            var snapshotDayIntoQuarter = s.Date.DayNumber - snapshotQuarterStart.DayNumber;
            return snapshotDayIntoQuarter <= dayIntoQuarter;
        }).ToList();
    }

    private static DateOnly GetPeriodStart(string period, int periodsBack)
    {
        var now = DateTime.UtcNow;
        return period.ToLower() switch
        {
            "daily" => DateOnly.FromDateTime(now.AddDays(-periodsBack)),
            "weekly" => DateOnly.FromDateTime(now.AddDays(-periodsBack * 7)),
            "monthly" => DateOnly.FromDateTime(now.AddMonths(-periodsBack)),
            "quarterly" => DateOnly.FromDateTime(now.AddMonths(-periodsBack * 3)),
            "yearly" => DateOnly.FromDateTime(now.AddYears(-periodsBack)),
            _ => DateOnly.FromDateTime(now.AddMonths(-periodsBack))
        };
    }

    private static IEnumerable<IGrouping<DateOnly, Income.Contracts.DTOs.SnapshotDto>> GroupByPeriod(
        List<Income.Contracts.DTOs.SnapshotDto> snapshots, string period)
    {
        return period.ToLower() switch
        {
            "daily" => snapshots.GroupBy(s => s.Date),
            "weekly" => snapshots.GroupBy(s => GetWeekStart(s.Date)),
            "monthly" => snapshots.GroupBy(s => new DateOnly(s.Date.Year, s.Date.Month, 1)),
            "quarterly" => snapshots.GroupBy(s => new DateOnly(s.Date.Year, GetQuarterStart(s.Date.Month), 1)),
            "yearly" => snapshots.GroupBy(s => new DateOnly(s.Date.Year, 1, 1)),
            _ => snapshots.GroupBy(s => new DateOnly(s.Date.Year, s.Date.Month, 1))
        };
    }

    private static DateOnly GetWeekStart(DateOnly date)
    {
        var daysToSubtract = (int)date.DayOfWeek;
        return date.AddDays(-daysToSubtract);
    }

    private static int GetQuarterStart(int month) => ((month - 1) / 3) * 3 + 1;
}
