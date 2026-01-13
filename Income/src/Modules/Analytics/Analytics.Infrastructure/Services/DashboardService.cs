using Analytics.Application.Services;
using Analytics.Contracts.Queries;
using FluentResults;

namespace Analytics.Infrastructure.Services;

internal sealed class DashboardService(
    IGetPortfolioSummaryHandler summaryHandler,
    IGetIncomeTimeSeriesHandler timeSeriesHandler,
    IGetDistributionHandler distributionHandler,
    IGetTopPerformersHandler topPerformersHandler,
    IGetPeriodComparisonHandler periodComparisonHandler,
    IGetDailyRateHandler dailyRateHandler,
    IGetStackedTimeSeriesHandler stackedTimeSeriesHandler,
    IGetStreamTrendsHandler streamTrendsHandler,
    IGetProjectionHandler projectionHandler,
    IGetTrendHandler trendHandler) : IDashboardService
{
    public async Task<Result<DashboardSummary>> GetSummaryAsync(int? streamType = null, CancellationToken ct = default)
    {
        var summaryResult = await summaryHandler.HandleAsync(new GetPortfolioSummaryQuery(StreamType: streamType), ct);
        if (summaryResult.IsFailed)
            return summaryResult.ToResult<DashboardSummary>();

        var momResult = await periodComparisonHandler.HandleAsync(
            new GetPeriodComparisonQuery("MoM", null, StreamType: streamType), ct);

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
        int? streamType = null,
        CancellationToken ct = default)
    {
        var query = new GetIncomeTimeSeriesQuery(startDate, endDate, granularity, StreamType: streamType)
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
        int? streamType = null,
        CancellationToken ct = default)
    {
        var query = new GetDistributionQuery(groupBy, StreamType: streamType)
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
        int? streamType = null,
        CancellationToken ct = default)
    {
        var query = new GetTopPerformersQuery(topN, StreamType: streamType);
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
        int? streamType = null,
        CancellationToken ct = default)
    {
        var query = new GetPeriodComparisonQuery(comparisonType, null, StreamType: streamType);
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

    public async Task<Result<DashboardKpis>> GetKpisAsync(int? streamType = null, CancellationToken ct = default)
    {
        var dailyRateResult = await dailyRateHandler.HandleAsync(new GetDailyRateQuery(30, StreamType: streamType), ct);
        if (dailyRateResult.IsFailed)
            return dailyRateResult.ToResult<DashboardKpis>();

        var trendResult = await trendHandler.HandleAsync(new GetTrendQuery("Monthly", 2, StreamType: streamType), ct);
        if (trendResult.IsFailed)
            return trendResult.ToResult<DashboardKpis>();

        var projectionResult = await projectionHandler.HandleAsync(new GetProjectionQuery(6, StreamType: streamType), ct);
        if (projectionResult.IsFailed)
            return projectionResult.ToResult<DashboardKpis>();

        var dailyRate = dailyRateResult.Value;
        var trend = trendResult.Value;
        var projection = projectionResult.Value;

        return Result.Ok(new DashboardKpis(
            DailyRate: new DailyRate(
                AverageDailyUsd: dailyRate.AverageDailyUsd,
                MedianDailyUsd: dailyRate.MedianDailyUsd,
                DaysAnalyzed: dailyRate.DaysAnalyzed,
                StandardDeviation: dailyRate.StandardDeviation,
                CoefficientOfVariation: dailyRate.CoefficientOfVariation),
            Trend: new TrendIndicator(
                ChangePercentage: trend.GrowthRatePercentage,
                Direction: trend.Direction,
                ComparisonPeriod: "vs last month"),
            Projection: new ProjectionSummary(
                Projected6MonthTotalUsd: projection.Projected6MonthTotalUsd,
                ConfidenceScore: projection.ConfidenceScore)));
    }

    public async Task<Result<StackedTimeSeries>> GetStackedTimeSeriesAsync(
        string granularity = "Daily",
        int periodsBack = 180,
        int? streamType = null,
        CancellationToken ct = default)
    {
        var query = new GetStackedTimeSeriesQuery(granularity, periodsBack, StreamType: streamType);
        var result = await stackedTimeSeriesHandler.HandleAsync(query, ct);
        if (result.IsFailed)
            return result.ToResult<StackedTimeSeries>();

        var data = result.Value;
        return Result.Ok(new StackedTimeSeries(
            Points: data.Points.Select(p => new StackedPoint(
                Date: p.Date,
                TotalUsd: p.TotalUsd,
                Streams: p.Streams.Select(s => new StreamContribution(
                    StreamId: s.StreamId,
                    StreamName: s.StreamName,
                    Category: s.Category,
                    AmountUsd: s.AmountUsd)).ToList())).ToList(),
            StreamNames: data.StreamNames,
            StartDate: data.StartDate,
            EndDate: data.EndDate,
            TotalUsd: data.TotalUsd));
    }

    public async Task<Result<StreamHealthSummary>> GetStreamHealthAsync(
        string comparisonType = "MoM",
        int? streamType = null,
        CancellationToken ct = default)
    {
        var query = new GetStreamTrendsQuery(comparisonType, StreamType: streamType);
        var result = await streamTrendsHandler.HandleAsync(query, ct);
        if (result.IsFailed)
            return result.ToResult<StreamHealthSummary>();

        var data = result.Value;
        return Result.Ok(new StreamHealthSummary(
            Streams: data.Streams.Select(s => new StreamHealthItem(
                StreamId: s.StreamId,
                StreamName: s.StreamName,
                Category: s.Category,
                ProviderName: s.ProviderName,
                CurrentPeriodUsd: s.CurrentPeriodUsd,
                PreviousPeriodUsd: s.PreviousPeriodUsd,
                ChangeUsd: s.ChangeUsd,
                ChangePercentage: s.ChangePercentage,
                Direction: s.Direction)).ToList(),
            GrowingCount: data.GrowingCount,
            DecliningCount: data.DecliningCount,
            StableCount: data.StableCount));
    }
}
