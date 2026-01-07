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
        var streamsResult = await streamsHandler.HandleAsync(new GetAllStreamsQuery(), ct);
        if (streamsResult.IsFailed)
            return streamsResult.ToResult<DailyRateDto>();

        var streams = streamsResult.Value;
        var now = DateOnly.FromDateTime(DateTime.UtcNow);
        var fromDate = now.AddDays(-query.DaysBack);

        var dailyTotals = streams
            .SelectMany(s => s.Snapshots)
            .Where(s => s.Date >= fromDate && s.Date <= now)
            .GroupBy(s => s.Date)
            .Select(g => new { Date = g.Key, Total = g.Sum(s => s.UsdAmount) })
            .OrderBy(x => x.Date)
            .ToList();

        if (dailyTotals.Count == 0)
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

        var amounts = dailyTotals.Select(x => x.Total).ToList();
        var average = amounts.Average(x => (double)x);
        var median = CalculateMedian(amounts);
        var variance = amounts.Average(x => Math.Pow((double)x - average, 2));
        var stdDev = Math.Sqrt(variance);
        var cv = average > 0 ? stdDev / average : 0;

        return Result.Ok(new DailyRateDto(
            AverageDailyUsd: Math.Round((decimal)average, 2),
            MedianDailyUsd: Math.Round(median, 2),
            DaysAnalyzed: dailyTotals.Count,
            StandardDeviation: Math.Round((decimal)stdDev, 2),
            CoefficientOfVariation: Math.Round((decimal)cv, 2),
            FromDate: dailyTotals.Min(x => x.Date),
            ToDate: dailyTotals.Max(x => x.Date)));
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
