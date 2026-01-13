using Analytics.Contracts.DTOs;
using Analytics.Contracts.Queries;
using FluentResults;
using Income.Contracts.Queries;

namespace Analytics.Infrastructure.Handlers;

internal sealed class GetTopPerformersHandler(
    IGetAllStreamsHandler streamsHandler,
    IGetAllProvidersHandler providersHandler) : IGetTopPerformersHandler
{
    public async Task<Result<TopPerformersDto>> HandleAsync(GetTopPerformersQuery query, CancellationToken ct = default)
    {
        var streamsResult = await streamsHandler.HandleAsync(new GetAllStreamsQuery(query.StreamType), ct);
        if (streamsResult.IsFailed)
            return streamsResult.ToResult<TopPerformersDto>();

        var providersResult = await providersHandler.HandleAsync(new GetAllProvidersQuery(), ct);
        if (providersResult.IsFailed)
            return providersResult.ToResult<TopPerformersDto>();

        var streams = streamsResult.Value;
        var providers = providersResult.Value.ToDictionary(p => p.Id, p => p);

        var streamStats = streams
            .Select(stream =>
            {
                var snapshots = stream.Snapshots
                    .Where(s => (!query.StartDate.HasValue || s.Date >= query.StartDate.Value) &&
                                (!query.EndDate.HasValue || s.Date <= query.EndDate.Value))
                    .ToList();

                return new
                {
                    Stream = stream,
                    TotalUsd = snapshots.Sum(s => s.UsdAmount),
                    SnapshotCount = snapshots.Count,
                    ProviderName = providers.TryGetValue(stream.ProviderId, out var p) ? p.Name : stream.ProviderId
                };
            })
            .Where(x => x.SnapshotCount > 0)
            .OrderByDescending(x => x.TotalUsd)
            .Take(query.TopN)
            .ToList();

        var totalUsd = streamStats.Sum(x => x.TotalUsd);

        var items = streamStats
            .Select((x, index) => new TopPerformerItemDto(
                StreamId: x.Stream.Id,
                StreamName: x.Stream.Name,
                ProviderId: x.Stream.ProviderId,
                ProviderName: x.ProviderName,
                Category: x.Stream.Category,
                TotalUsd: x.TotalUsd,
                Percentage: totalUsd > 0 ? Math.Round(x.TotalUsd / totalUsd * 100, 2) : 0,
                AveragePerSnapshotUsd: x.SnapshotCount > 0 ? Math.Round(x.TotalUsd / x.SnapshotCount, 2) : 0,
                SnapshotCount: x.SnapshotCount,
                Rank: index + 1))
            .ToList();

        return Result.Ok(new TopPerformersDto(Items: items, TotalUsd: totalUsd));
    }
}
