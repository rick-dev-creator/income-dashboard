using Analytics.Contracts.DTOs;
using Analytics.Contracts.Queries;
using FluentResults;
using Income.Contracts.DTOs;
using Income.Contracts.Queries;

namespace Analytics.Infrastructure.Handlers;

internal sealed class GetIncomeTimeSeriesHandler(
    IGetAllStreamsHandler streamsHandler) : IGetIncomeTimeSeriesHandler
{
    public async Task<Result<TimeSeriesDto>> HandleAsync(GetIncomeTimeSeriesQuery query, CancellationToken ct = default)
    {
        var streamsResult = await streamsHandler.HandleAsync(new GetAllStreamsQuery(), ct);
        if (streamsResult.IsFailed)
            return streamsResult.ToResult<TimeSeriesDto>();

        var streams = streamsResult.Value;

        var filteredSnapshots = streams
            .Where(s => query.StreamId is null || s.Id == query.StreamId)
            .Where(s => query.ProviderId is null || s.ProviderId == query.ProviderId)
            .Where(s => query.Category is null || s.Category == query.Category)
            .SelectMany(s => s.Snapshots)
            .Where(snap => snap.Date >= query.StartDate && snap.Date <= query.EndDate)
            .ToList();

        var groupedPoints = GroupByGranularity(filteredSnapshots, query.Granularity);

        var points = groupedPoints
            .Select(g => new TimeSeriesPointDto(
                Date: g.Key,
                AmountUsd: g.Sum(s => s.UsdAmount),
                SnapshotCount: g.Count()))
            .OrderBy(p => p.Date)
            .ToList();

        var totalUsd = points.Sum(p => p.AmountUsd);

        return Result.Ok(new TimeSeriesDto(
            Points: points,
            Granularity: query.Granularity,
            StartDate: query.StartDate,
            EndDate: query.EndDate,
            TotalUsd: totalUsd,
            AverageUsd: points.Count > 0 ? totalUsd / points.Count : 0,
            MinUsd: points.Count > 0 ? points.Min(p => p.AmountUsd) : 0,
            MaxUsd: points.Count > 0 ? points.Max(p => p.AmountUsd) : 0));
    }

    private static IEnumerable<IGrouping<DateOnly, SnapshotDto>> GroupByGranularity(
        List<SnapshotDto> snapshots, string granularity)
    {
        return granularity.ToLower() switch
        {
            "daily" => snapshots.GroupBy(s => s.Date),
            "weekly" => snapshots.GroupBy(s => GetWeekStart(s.Date)),
            "monthly" => snapshots.GroupBy(s => new DateOnly(s.Date.Year, s.Date.Month, 1)),
            "quarterly" => snapshots.GroupBy(s => new DateOnly(s.Date.Year, GetQuarterStart(s.Date.Month), 1)),
            "yearly" => snapshots.GroupBy(s => new DateOnly(s.Date.Year, 1, 1)),
            _ => snapshots.GroupBy(s => s.Date)
        };
    }

    private static DateOnly GetWeekStart(DateOnly date)
    {
        var daysToSubtract = (int)date.DayOfWeek;
        return date.AddDays(-daysToSubtract);
    }

    private static int GetQuarterStart(int month) => ((month - 1) / 3) * 3 + 1;
}
