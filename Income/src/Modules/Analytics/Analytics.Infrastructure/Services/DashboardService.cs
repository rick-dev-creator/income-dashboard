using Analytics.Application.Services;
using Analytics.Contracts.Queries;
using FluentResults;

namespace Analytics.Infrastructure.Services;

internal sealed class DashboardService(
    IGetPortfolioSummaryHandler summaryHandler,
    IGetIncomeTimeSeriesHandler timeSeriesHandler,
    IGetDistributionHandler distributionHandler,
    IGetTopPerformersHandler topPerformersHandler,
    IGetPeriodComparisonHandler periodComparisonHandler) : IDashboardService
{
    public async Task<Result<DashboardSummary>> GetSummaryAsync(CancellationToken ct = default)
    {
        var summaryResult = await summaryHandler.HandleAsync(new GetPortfolioSummaryQuery(), ct);
        if (summaryResult.IsFailed)
            return summaryResult.ToResult<DashboardSummary>();

        var momResult = await periodComparisonHandler.HandleAsync(
            new GetPeriodComparisonQuery("MoM", null), ct);

        var summary = summaryResult.Value;
        PeriodComparison? mom = null;

        if (momResult.IsSuccess)
        {
            var momData = momResult.Value;
            mom = new PeriodComparison(
                ComparisonType: "MoM",
                CurrentPeriod: $"{momData.CurrentPeriod.StartDate:MMM yyyy}",
                PreviousPeriod: $"{momData.PreviousPeriod.StartDate:MMM yyyy}",
                CurrentAmountUsd: momData.CurrentPeriod.TotalUsd,
                PreviousAmountUsd: momData.PreviousPeriod.TotalUsd,
                ChangeUsd: momData.ChangeUsd,
                ChangePercentage: momData.ChangePercentage,
                Trend: momData.Trend);
        }

        return Result.Ok(new DashboardSummary(
            TotalIncomeUsd: summary.TotalIncomeUsd,
            FixedMonthlyIncomeUsd: summary.FixedMonthlyIncomeUsd,
            ActiveStreamCount: summary.ActiveStreamCount,
            ProviderCount: summary.ProviderCount,
            MonthOverMonth: mom));
    }

    public async Task<Result<IncomeTimeSeries>> GetTimeSeriesAsync(
        DateOnly startDate,
        DateOnly endDate,
        string granularity,
        string? category = null,
        CancellationToken ct = default)
    {
        var query = new GetIncomeTimeSeriesQuery(startDate, endDate, granularity)
        {
            Category = category
        };

        var result = await timeSeriesHandler.HandleAsync(query, ct);
        if (result.IsFailed)
            return result.ToResult<IncomeTimeSeries>();

        var data = result.Value;
        return Result.Ok(new IncomeTimeSeries(
            Granularity: data.Granularity,
            Points: data.Points.Select(p => new TimeSeriesPoint(p.Date, p.AmountUsd)).ToList(),
            TotalUsd: data.TotalUsd,
            AverageUsd: data.AverageUsd,
            MinUsd: data.MinUsd,
            MaxUsd: data.MaxUsd));
    }

    public async Task<Result<IncomeDistribution>> GetDistributionAsync(
        string groupBy,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        CancellationToken ct = default)
    {
        var query = new GetDistributionQuery(groupBy)
        {
            StartDate = startDate,
            EndDate = endDate
        };

        var result = await distributionHandler.HandleAsync(query, ct);
        if (result.IsFailed)
            return result.ToResult<IncomeDistribution>();

        var data = result.Value;
        return Result.Ok(new IncomeDistribution(
            GroupBy: groupBy,
            Items: data.Items.Select(i => new DistributionItem(i.Key, i.Label, i.AmountUsd, i.Percentage)).ToList(),
            TotalUsd: data.TotalUsd));
    }

    public async Task<Result<IReadOnlyList<TopPerformerItem>>> GetTopPerformersAsync(
        int topN = 5,
        CancellationToken ct = default)
    {
        var query = new GetTopPerformersQuery(topN);
        var result = await topPerformersHandler.HandleAsync(query, ct);
        if (result.IsFailed)
            return result.ToResult<IReadOnlyList<TopPerformerItem>>();

        var data = result.Value;
        return Result.Ok<IReadOnlyList<TopPerformerItem>>(
            data.Items.Select(i => new TopPerformerItem(
                Rank: i.Rank,
                StreamId: i.StreamId,
                StreamName: i.StreamName,
                Category: i.Category,
                ProviderName: i.ProviderName,
                TotalUsd: i.TotalUsd,
                Percentage: i.Percentage)).ToList());
    }

    public async Task<Result<PeriodComparison>> GetPeriodComparisonAsync(
        string comparisonType,
        CancellationToken ct = default)
    {
        var query = new GetPeriodComparisonQuery(comparisonType, null);
        var result = await periodComparisonHandler.HandleAsync(query, ct);
        if (result.IsFailed)
            return result.ToResult<PeriodComparison>();

        var data = result.Value;
        return Result.Ok(new PeriodComparison(
            ComparisonType: comparisonType,
            CurrentPeriod: $"{data.CurrentPeriod.StartDate:MMM yyyy}",
            PreviousPeriod: $"{data.PreviousPeriod.StartDate:MMM yyyy}",
            CurrentAmountUsd: data.CurrentPeriod.TotalUsd,
            PreviousAmountUsd: data.PreviousPeriod.TotalUsd,
            ChangeUsd: data.ChangeUsd,
            ChangePercentage: data.ChangePercentage,
            Trend: data.Trend));
    }
}
