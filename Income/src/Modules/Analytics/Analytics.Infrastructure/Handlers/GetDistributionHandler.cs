using Analytics.Contracts.DTOs;
using Analytics.Contracts.Queries;
using FluentResults;
using Income.Contracts.DTOs;
using Income.Contracts.Queries;

namespace Analytics.Infrastructure.Handlers;

internal sealed class GetDistributionHandler(
    IGetAllStreamsHandler streamsHandler,
    IGetAllProvidersHandler providersHandler) : IGetDistributionHandler
{
    public async Task<Result<DistributionDto>> HandleAsync(GetDistributionQuery query, CancellationToken ct = default)
    {
        var streamsResult = await streamsHandler.HandleAsync(new GetAllStreamsQuery(query.StreamType), ct);
        if (streamsResult.IsFailed)
            return streamsResult.ToResult<DistributionDto>();

        var providersResult = await providersHandler.HandleAsync(new GetAllProvidersQuery(), ct);
        if (providersResult.IsFailed)
            return providersResult.ToResult<DistributionDto>();

        var streams = streamsResult.Value;
        var providers = providersResult.Value.ToDictionary(p => p.Id, p => p);

        var snapshotsWithStreams = streams
            .SelectMany(s => s.Snapshots.Select(snap => new { Stream = s, Snapshot = snap }))
            .Where(x => (!query.StartDate.HasValue || x.Snapshot.Date >= query.StartDate.Value) &&
                        (!query.EndDate.HasValue || x.Snapshot.Date <= query.EndDate.Value))
            .ToList();

        var totalUsd = snapshotsWithStreams.Sum(x => x.Snapshot.UsdAmount);

        var grouped = query.GroupBy.ToLower() switch
        {
            "category" => snapshotsWithStreams
                .GroupBy(x => x.Stream.Category)
                .Select(g => new DistributionItemDto(
                    Key: g.Key,
                    Label: g.Key,
                    AmountUsd: g.Sum(x => x.Snapshot.UsdAmount),
                    Percentage: totalUsd > 0 ? g.Sum(x => x.Snapshot.UsdAmount) / totalUsd * 100 : 0,
                    SnapshotCount: g.Count())),

            "provider" => snapshotsWithStreams
                .GroupBy(x => x.Stream.ProviderId)
                .Select(g => new DistributionItemDto(
                    Key: g.Key,
                    Label: providers.TryGetValue(g.Key, out var p) ? p.Name : g.Key,
                    AmountUsd: g.Sum(x => x.Snapshot.UsdAmount),
                    Percentage: totalUsd > 0 ? g.Sum(x => x.Snapshot.UsdAmount) / totalUsd * 100 : 0,
                    SnapshotCount: g.Count())),

            "stream" => snapshotsWithStreams
                .GroupBy(x => x.Stream.Id)
                .Select(g => new DistributionItemDto(
                    Key: g.Key,
                    Label: g.First().Stream.Name,
                    AmountUsd: g.Sum(x => x.Snapshot.UsdAmount),
                    Percentage: totalUsd > 0 ? g.Sum(x => x.Snapshot.UsdAmount) / totalUsd * 100 : 0,
                    SnapshotCount: g.Count())),

            "currency" => snapshotsWithStreams
                .GroupBy(x => x.Snapshot.OriginalCurrency)
                .Select(g => new DistributionItemDto(
                    Key: g.Key,
                    Label: g.Key,
                    AmountUsd: g.Sum(x => x.Snapshot.UsdAmount),
                    Percentage: totalUsd > 0 ? g.Sum(x => x.Snapshot.UsdAmount) / totalUsd * 100 : 0,
                    SnapshotCount: g.Count())),

            _ => []
        };

        var items = grouped.OrderByDescending(x => x.AmountUsd).ToList();

        return Result.Ok(new DistributionDto(Items: items, TotalUsd: totalUsd));
    }
}
