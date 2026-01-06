using Analytics.Contracts.DTOs;
using Analytics.Contracts.Queries;
using FluentResults;
using Income.Contracts.DTOs;
using Income.Contracts.Queries;

namespace Analytics.Infrastructure.Handlers;

internal sealed class GetPortfolioSummaryHandler(
    IGetAllStreamsHandler streamsHandler,
    IGetAllProvidersHandler providersHandler) : IGetPortfolioSummaryHandler
{
    public async Task<Result<PortfolioSummaryDto>> HandleAsync(GetPortfolioSummaryQuery query, CancellationToken ct = default)
    {
        var streamsResult = await streamsHandler.HandleAsync(new GetAllStreamsQuery(), ct);
        if (streamsResult.IsFailed)
            return streamsResult.ToResult<PortfolioSummaryDto>();

        var providersResult = await providersHandler.HandleAsync(new GetAllProvidersQuery(), ct);
        if (providersResult.IsFailed)
            return providersResult.ToResult<PortfolioSummaryDto>();

        var streams = streamsResult.Value;
        var providers = providersResult.Value;

        var allSnapshots = streams
            .SelectMany(s => s.Snapshots)
            .Where(snap => (!query.StartDate.HasValue || snap.Date >= query.StartDate.Value) &&
                           (!query.EndDate.HasValue || snap.Date <= query.EndDate.Value))
            .ToList();

        if (allSnapshots.Count == 0)
        {
            return Result.Ok(new PortfolioSummaryDto(
                TotalIncomeUsd: 0,
                StreamCount: streams.Count,
                ActiveStreamCount: streams.Count(s => s.SyncStatus.State == "Active"),
                ProviderCount: providers.Count,
                AverageIncomePerStreamUsd: 0,
                FixedMonthlyIncomeUsd: 0,
                VariableMonthlyIncomeUsd: 0,
                EarliestSnapshotDate: DateOnly.FromDateTime(DateTime.UtcNow),
                LatestSnapshotDate: DateOnly.FromDateTime(DateTime.UtcNow)));
        }

        var totalIncome = allSnapshots.Sum(s => s.UsdAmount);
        var activeStreams = streams.Where(s => s.SyncStatus.State == "Active").ToList();
        var fixedStreams = streams.Where(s => s.IsFixed).ToList();
        var variableStreams = streams.Where(s => !s.IsFixed).ToList();

        var fixedMonthlyIncome = CalculateMonthlyFixedIncome(fixedStreams);
        var variableMonthlyIncome = CalculateMonthlyVariableIncome(variableStreams);

        return Result.Ok(new PortfolioSummaryDto(
            TotalIncomeUsd: totalIncome,
            StreamCount: streams.Count,
            ActiveStreamCount: activeStreams.Count,
            ProviderCount: providers.Count,
            AverageIncomePerStreamUsd: streams.Count > 0 ? totalIncome / streams.Count : 0,
            FixedMonthlyIncomeUsd: fixedMonthlyIncome,
            VariableMonthlyIncomeUsd: variableMonthlyIncome,
            EarliestSnapshotDate: allSnapshots.Min(s => s.Date),
            LatestSnapshotDate: allSnapshots.Max(s => s.Date)));
    }

    private static decimal CalculateMonthlyFixedIncome(IReadOnlyList<StreamDto> fixedStreams)
    {
        decimal total = 0;
        foreach (var stream in fixedStreams)
        {
            var lastSnapshot = stream.Snapshots.MaxBy(s => s.Date);
            if (lastSnapshot is null) continue;

            var multiplier = stream.FixedPeriod?.ToLower() switch
            {
                "daily" => 30m,
                "weekly" => 4.33m,
                "biweekly" => 2.17m,
                "monthly" => 1m,
                "quarterly" => 0.33m,
                "annually" => 0.083m,
                _ => 1m
            };

            total += lastSnapshot.UsdAmount * multiplier;
        }
        return total;
    }

    private static decimal CalculateMonthlyVariableIncome(IReadOnlyList<StreamDto> variableStreams)
    {
        var now = DateTime.UtcNow;
        var startOfLastMonth = new DateOnly(now.Year, now.Month, 1).AddMonths(-1);
        var endOfLastMonth = startOfLastMonth.AddMonths(1).AddDays(-1);

        return variableStreams
            .SelectMany(s => s.Snapshots)
            .Where(s => s.Date >= startOfLastMonth && s.Date <= endOfLastMonth)
            .Sum(s => s.UsdAmount);
    }
}
