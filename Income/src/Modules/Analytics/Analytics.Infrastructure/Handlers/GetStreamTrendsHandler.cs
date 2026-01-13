using Analytics.Contracts.DTOs;
using Analytics.Contracts.Queries;
using FluentResults;
using Income.Contracts.Queries;

namespace Analytics.Infrastructure.Handlers;

internal sealed class GetStreamTrendsHandler(
    IGetAllStreamsHandler streamsHandler,
    IGetAllProvidersHandler providersHandler) : IGetStreamTrendsHandler
{
    public async Task<Result<StreamTrendsDto>> HandleAsync(GetStreamTrendsQuery query, CancellationToken ct = default)
    {
        var streamsResult = await streamsHandler.HandleAsync(new GetAllStreamsQuery(query.StreamType), ct);
        if (streamsResult.IsFailed)
            return streamsResult.ToResult<StreamTrendsDto>();

        var providersResult = await providersHandler.HandleAsync(new GetAllProvidersQuery(), ct);
        if (providersResult.IsFailed)
            return providersResult.ToResult<StreamTrendsDto>();

        var streams = streamsResult.Value;
        var providers = providersResult.Value.ToDictionary(p => p.Id, p => p.Name);
        var (currentStart, currentEnd, previousStart, previousEnd) = GetPeriodBoundaries(query.ComparisonType);

        var trends = new List<StreamTrendItemDto>();

        foreach (var stream in streams)
        {
            var currentPeriodTotal = stream.Snapshots
                .Where(s => s.Date >= currentStart && s.Date <= currentEnd)
                .Sum(s => s.UsdAmount);

            var previousPeriodTotal = stream.Snapshots
                .Where(s => s.Date >= previousStart && s.Date <= previousEnd)
                .Sum(s => s.UsdAmount);

            var changeUsd = currentPeriodTotal - previousPeriodTotal;
            var changePercentage = previousPeriodTotal != 0
                ? (changeUsd / previousPeriodTotal) * 100
                : currentPeriodTotal > 0 ? 100 : 0;

            var direction = changePercentage switch
            {
                > 5 => "Upward",
                < -5 => "Downward",
                _ => "Stable"
            };

            providers.TryGetValue(stream.ProviderId, out var providerName);

            trends.Add(new StreamTrendItemDto(
                StreamId: stream.Id,
                StreamName: stream.Name,
                Category: stream.Category,
                ProviderName: providerName ?? "Unknown",
                CurrentPeriodUsd: Math.Round(currentPeriodTotal, 2),
                PreviousPeriodUsd: Math.Round(previousPeriodTotal, 2),
                ChangeUsd: Math.Round(changeUsd, 2),
                ChangePercentage: Math.Round(changePercentage, 2),
                Direction: direction));
        }

        var orderedTrends = trends
            .OrderByDescending(t => Math.Abs(t.ChangePercentage))
            .ToList();

        return Result.Ok(new StreamTrendsDto(
            Streams: orderedTrends,
            GrowingCount: orderedTrends.Count(t => t.Direction == "Upward"),
            DecliningCount: orderedTrends.Count(t => t.Direction == "Downward"),
            StableCount: orderedTrends.Count(t => t.Direction == "Stable")));
    }

    private static (DateOnly CurrentStart, DateOnly CurrentEnd, DateOnly PreviousStart, DateOnly PreviousEnd)
        GetPeriodBoundaries(string comparisonType)
    {
        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);

        return comparisonType.ToUpper() switch
        {
            "MOM" => (
                new DateOnly(now.Year, now.Month, 1),
                today,
                new DateOnly(now.Year, now.Month, 1).AddMonths(-1),
                new DateOnly(now.Year, now.Month, 1).AddDays(-1)),
            "WOW" => (
                today.AddDays(-(int)today.DayOfWeek),
                today,
                today.AddDays(-(int)today.DayOfWeek - 7),
                today.AddDays(-(int)today.DayOfWeek - 1)),
            "YOY" => (
                new DateOnly(now.Year, 1, 1),
                today,
                new DateOnly(now.Year - 1, 1, 1),
                new DateOnly(now.Year - 1, 12, 31)),
            "QOQ" => GetQuarterBoundaries(now),
            _ => (
                new DateOnly(now.Year, now.Month, 1),
                today,
                new DateOnly(now.Year, now.Month, 1).AddMonths(-1),
                new DateOnly(now.Year, now.Month, 1).AddDays(-1))
        };
    }

    private static (DateOnly CurrentStart, DateOnly CurrentEnd, DateOnly PreviousStart, DateOnly PreviousEnd)
        GetQuarterBoundaries(DateTime now)
    {
        var currentQuarter = (now.Month - 1) / 3;
        var currentQuarterStart = new DateOnly(now.Year, currentQuarter * 3 + 1, 1);
        var previousQuarterStart = currentQuarterStart.AddMonths(-3);

        return (
            currentQuarterStart,
            DateOnly.FromDateTime(now),
            previousQuarterStart,
            currentQuarterStart.AddDays(-1));
    }
}
