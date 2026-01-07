using Analytics.Contracts.DTOs;
using Analytics.Contracts.Queries;
using FluentResults;
using Income.Contracts.DTOs;
using Income.Contracts.Queries;

namespace Analytics.Infrastructure.Handlers;

internal sealed class GetStackedTimeSeriesHandler(
    IGetAllStreamsHandler streamsHandler) : IGetStackedTimeSeriesHandler
{
    public async Task<Result<StackedTimeSeriesDto>> HandleAsync(GetStackedTimeSeriesQuery query, CancellationToken ct = default)
    {
        var streamsResult = await streamsHandler.HandleAsync(new GetAllStreamsQuery(), ct);
        if (streamsResult.IsFailed)
            return streamsResult.ToResult<StackedTimeSeriesDto>();

        var streams = streamsResult.Value;
        var now = DateTime.UtcNow;
        var startDate = GetPeriodStart(query.Granularity, query.PeriodsBack);

        var allSnapshots = streams
            .SelectMany(s => s.Snapshots.Select(snap => new
            {
                StreamId = s.Id,
                StreamName = s.Name,
                Category = s.Category,
                Snapshot = snap
            }))
            .Where(x => x.Snapshot.Date >= startDate)
            .ToList();

        var groupedByPeriod = GroupByPeriod(allSnapshots, query.Granularity)
            .OrderBy(g => g.Key)
            .ToList();

        var streamNames = streams.Select(s => s.Name).Distinct().ToList();
        var points = new List<StackedPointDto>();

        foreach (var periodGroup in groupedByPeriod)
        {
            var streamContributions = periodGroup
                .GroupBy(x => new { x.StreamId, x.StreamName, x.Category })
                .Select(g => new StreamContributionDto(
                    StreamId: g.Key.StreamId,
                    StreamName: g.Key.StreamName,
                    Category: g.Key.Category,
                    AmountUsd: Math.Round(g.Sum(x => x.Snapshot.UsdAmount), 2)))
                .OrderByDescending(x => x.AmountUsd)
                .ToList();

            points.Add(new StackedPointDto(
                Date: periodGroup.Key,
                TotalUsd: Math.Round(streamContributions.Sum(x => x.AmountUsd), 2),
                Streams: streamContributions));
        }

        var actualStart = points.Count > 0 ? points.Min(p => p.Date) : startDate;
        var actualEnd = points.Count > 0 ? points.Max(p => p.Date) : DateOnly.FromDateTime(now);

        return Result.Ok(new StackedTimeSeriesDto(
            Points: points,
            StreamNames: streamNames,
            StartDate: actualStart,
            EndDate: actualEnd,
            TotalUsd: Math.Round(points.Sum(p => p.TotalUsd), 2)));
    }

    private static DateOnly GetPeriodStart(string granularity, int periodsBack)
    {
        var now = DateTime.UtcNow;
        return granularity.ToLower() switch
        {
            "daily" => DateOnly.FromDateTime(now.AddDays(-periodsBack)),
            "weekly" => DateOnly.FromDateTime(now.AddDays(-periodsBack * 7)),
            "monthly" => DateOnly.FromDateTime(now.AddMonths(-periodsBack)),
            _ => DateOnly.FromDateTime(now.AddDays(-periodsBack))
        };
    }

    private static IEnumerable<IGrouping<DateOnly, T>> GroupByPeriod<T>(
        List<T> items, string granularity) where T : class
    {
        Func<T, DateOnly> getDate = item =>
        {
            var prop = item.GetType().GetProperty("Snapshot");
            var snapshot = prop?.GetValue(item) as SnapshotDto;
            return snapshot?.Date ?? DateOnly.MinValue;
        };

        return granularity.ToLower() switch
        {
            "daily" => items.GroupBy(x => getDate(x)),
            "weekly" => items.GroupBy(x => GetWeekStart(getDate(x))),
            "monthly" => items.GroupBy(x => new DateOnly(getDate(x).Year, getDate(x).Month, 1)),
            _ => items.GroupBy(x => getDate(x))
        };
    }

    private static DateOnly GetWeekStart(DateOnly date)
    {
        var daysToSubtract = (int)date.DayOfWeek;
        return date.AddDays(-daysToSubtract);
    }
}
