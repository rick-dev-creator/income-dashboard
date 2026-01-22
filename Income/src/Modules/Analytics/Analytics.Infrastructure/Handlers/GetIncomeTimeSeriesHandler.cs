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
        var streamsResult = await streamsHandler.HandleAsync(new GetAllStreamsQuery(query.StreamType, query.ProviderId), ct);
        if (streamsResult.IsFailed)
            return streamsResult.ToResult<TimeSeriesDto>();

        var streams = streamsResult.Value;

        var filteredSnapshots = streams
            .Where(s => query.StreamId is null || s.Id == query.StreamId)
            .Where(s => query.ProviderId is null || s.ProviderId == query.ProviderId)
            .Where(s => query.Category is null || s.Category == query.Category)
            .SelectMany(s => s.Snapshots.Select(snap => (s.StreamType, Snapshot: snap)))
            .Where(x => x.Snapshot.Date >= query.StartDate && x.Snapshot.Date <= query.EndDate)
            .ToList();

        var groupedPoints = GroupByGranularity(filteredSnapshots, query.Granularity, query.StreamType);

        var points = groupedPoints
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

    private static List<TimeSeriesPointDto> GroupByGranularity(
        List<(int StreamType, SnapshotDto Snapshot)> snapshots, string granularity, int? streamTypeFilter)
    {
        Func<SnapshotDto, DateOnly> getGroupKey = granularity.ToLower() switch
        {
            "daily" => s => s.Date,
            "weekly" => s => GetWeekStart(s.Date),
            "monthly" => s => new DateOnly(s.Date.Year, s.Date.Month, 1),
            "quarterly" => s => new DateOnly(s.Date.Year, GetQuarterStart(s.Date.Month), 1),
            "yearly" => s => new DateOnly(s.Date.Year, 1, 1),
            _ => s => s.Date
        };

        var grouped = snapshots.GroupBy(x => getGroupKey(x.Snapshot));

        return grouped.Select(g =>
        {
            decimal amount;
            if (streamTypeFilter.HasValue)
            {
                // Filtered mode: just sum all
                amount = g.Sum(x => x.Snapshot.UsdAmount);
            }
            else
            {
                // Net Flow mode: Income - Outcome
                var income = g.Where(x => x.StreamType == 0).Sum(x => x.Snapshot.UsdAmount);
                var outcome = g.Where(x => x.StreamType == 1).Sum(x => x.Snapshot.UsdAmount);
                amount = income - outcome;
            }
            return new TimeSeriesPointDto(
                Date: g.Key,
                AmountUsd: amount,
                SnapshotCount: g.Count());
        }).ToList();
    }

    private static DateOnly GetWeekStart(DateOnly date)
    {
        var daysToSubtract = (int)date.DayOfWeek;
        return date.AddDays(-daysToSubtract);
    }

    private static int GetQuarterStart(int month) => ((month - 1) / 3) * 3 + 1;
}
