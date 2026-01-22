using Analytics.Contracts.DTOs;
using Analytics.Contracts.Queries;
using FluentResults;
using Income.Contracts.Queries;

namespace Analytics.Infrastructure.Handlers;

internal sealed class GetDailyRateHandler(
    IGetAllStreamsHandler streamsHandler) : IGetDailyRateHandler
{
    public async Task<Result<DailyRateDto>> HandleAsync(GetDailyRateQuery query, CancellationToken ct = default)
    {
        var streamsResult = await streamsHandler.HandleAsync(new GetAllStreamsQuery(query.StreamType, query.ProviderId), ct);
        if (streamsResult.IsFailed)
            return streamsResult.ToResult<DailyRateDto>();

        var streams = streamsResult.Value;
        var now = DateOnly.FromDateTime(DateTime.UtcNow);
        var fromDate = now.AddDays(-query.DaysBack);

        var allSnapshots = streams
            .SelectMany(s => s.Snapshots.Select(snap => new { s.StreamType, Snapshot = snap }))
            .Where(x => x.Snapshot.Date >= fromDate && x.Snapshot.Date <= now)
            .ToList();

        if (allSnapshots.Count == 0)
        {
            return Result.Ok(new DailyRateDto(
                AverageDailyUsd: 0,
                MedianDailyUsd: 0,
                DaysAnalyzed: 0,
                StandardDeviation: 0,
                CoefficientOfVariation: 0,
                FromDate: fromDate,
                ToDate: now));
        }

        // Calculate total based on mode:
        // - If StreamType filter is applied: sum all amounts
        // - If no filter (Net Flow mode): Income - Outcome
        decimal totalAmount;
        if (query.StreamType.HasValue)
        {
            totalAmount = allSnapshots.Sum(s => s.Snapshot.UsdAmount);
        }
        else
        {
            var incomeTotal = allSnapshots.Where(s => s.StreamType == 0).Sum(s => s.Snapshot.UsdAmount);
            var outcomeTotal = allSnapshots.Where(s => s.StreamType == 1).Sum(s => s.Snapshot.UsdAmount);
            totalAmount = incomeTotal - outcomeTotal;
        }

        var calendarDays = query.DaysBack;
        var average = (double)totalAmount / calendarDays;

        // For median and std dev, use daily totals (days with transactions)
        // In Net Flow mode, calculate daily net (Income - Outcome per day)
        List<decimal> dailyTotals;
        if (query.StreamType.HasValue)
        {
            dailyTotals = allSnapshots
                .GroupBy(s => s.Snapshot.Date)
                .Select(g => g.Sum(s => s.Snapshot.UsdAmount))
                .OrderBy(x => x)
                .ToList();
        }
        else
        {
            dailyTotals = allSnapshots
                .GroupBy(s => s.Snapshot.Date)
                .Select(g =>
                {
                    var dayIncome = g.Where(x => x.StreamType == 0).Sum(x => x.Snapshot.UsdAmount);
                    var dayOutcome = g.Where(x => x.StreamType == 1).Sum(x => x.Snapshot.UsdAmount);
                    return dayIncome - dayOutcome;
                })
                .OrderBy(x => x)
                .ToList();
        }

        var median = CalculateMedian(dailyTotals);
        var variance = dailyTotals.Count > 0
            ? dailyTotals.Average(x => Math.Pow((double)x - average, 2))
            : 0;
        var stdDev = Math.Sqrt(variance);
        var cv = average > 0 ? stdDev / average : 0;

        return Result.Ok(new DailyRateDto(
            AverageDailyUsd: Math.Round((decimal)average, 2),
            MedianDailyUsd: Math.Round(median, 2),
            DaysAnalyzed: calendarDays,
            StandardDeviation: Math.Round((decimal)stdDev, 2),
            CoefficientOfVariation: Math.Round((decimal)cv, 2),
            FromDate: fromDate,
            ToDate: now));
    }

    private static decimal CalculateMedian(List<decimal> values)
    {
        var sorted = values.OrderBy(x => x).ToList();
        var count = sorted.Count;

        if (count == 0) return 0;
        if (count % 2 == 0)
            return (sorted[count / 2 - 1] + sorted[count / 2]) / 2;

        return sorted[count / 2];
    }
}
