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
    IGetProjectionHandler projectionHandler) : IDashboardService
{
    public async Task<Result<DashboardSummary>> GetSummaryAsync(int? streamType = null, string? providerId = null, CancellationToken ct = default)
    {
        var summaryResult = await summaryHandler.HandleAsync(new GetPortfolioSummaryQuery(StreamType: streamType, ProviderId: providerId), ct);
        if (summaryResult.IsFailed)
            return summaryResult.ToResult<DashboardSummary>();

        var momResult = await periodComparisonHandler.HandleAsync(
            new GetPeriodComparisonQuery("MoM", null, StreamType: streamType, ProviderId: providerId), ct);

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
        string? providerId = null,
        CancellationToken ct = default)
    {
        var query = new GetIncomeTimeSeriesQuery(startDate, endDate, granularity, StreamType: streamType, ProviderId: providerId)
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
        string? providerId = null,
        CancellationToken ct = default)
    {
        var query = new GetDistributionQuery(groupBy, StreamType: streamType, ProviderId: providerId)
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
        string? providerId = null,
        CancellationToken ct = default)
    {
        var query = new GetTopPerformersQuery(topN, StreamType: streamType, ProviderId: providerId);
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
        string? providerId = null,
        CancellationToken ct = default)
    {
        var query = new GetPeriodComparisonQuery(comparisonType, null, StreamType: streamType, ProviderId: providerId);
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

    public async Task<Result<DashboardKpis>> GetKpisAsync(int? streamType = null, string? providerId = null, CancellationToken ct = default)
    {
        var dailyRateResult = await dailyRateHandler.HandleAsync(new GetDailyRateQuery(90, StreamType: streamType, ProviderId: providerId), ct);
        if (dailyRateResult.IsFailed)
            return dailyRateResult.ToResult<DashboardKpis>();

        // For KPI trend, use period comparison of COMPLETE months (last month vs month before)
        // This avoids confusing partial-month comparisons like "Jan 1-22 vs Dec 1-22"
        var now = DateTime.UtcNow;
        var lastMonthStart = new DateOnly(now.Year, now.Month, 1).AddMonths(-1);
        var periodComparisonResult = await periodComparisonHandler.HandleAsync(
            new GetPeriodComparisonQuery("MoM", lastMonthStart, StreamType: streamType, ProviderId: providerId), ct);

        var projectionResult = await projectionHandler.HandleAsync(new GetProjectionQuery(6, StreamType: streamType, ProviderId: providerId), ct);
        if (projectionResult.IsFailed)
            return projectionResult.ToResult<DashboardKpis>();

        var dailyRate = dailyRateResult.Value;
        var projection = projectionResult.Value;

        // Default trend values
        var changePercentage = 0m;
        var direction = "Stable";
        var comparisonPeriod = "vs previous month";

        if (periodComparisonResult.IsSuccess)
        {
            var comparison = periodComparisonResult.Value;
            changePercentage = comparison.ChangePercentage;
            direction = comparison.ChangePercentage switch
            {
                > 5 => "Upward",
                < -5 => "Downward",
                _ => "Stable"
            };
            // Show which complete months are being compared
            comparisonPeriod = $"{comparison.CurrentPeriod.StartDate:MMM} vs {comparison.PreviousPeriod.StartDate:MMM}";
        }

        return Result.Ok(new DashboardKpis(
            DailyRate: new DailyRate(
                AverageDailyUsd: dailyRate.AverageDailyUsd,
                MedianDailyUsd: dailyRate.MedianDailyUsd,
                DaysAnalyzed: dailyRate.DaysAnalyzed,
                StandardDeviation: dailyRate.StandardDeviation,
                CoefficientOfVariation: dailyRate.CoefficientOfVariation),
            Trend: new TrendIndicator(
                ChangePercentage: changePercentage,
                Direction: direction,
                ComparisonPeriod: comparisonPeriod),
            Projection: new ProjectionSummary(
                Projected6MonthTotalUsd: projection.Projected6MonthTotalUsd,
                ConfidenceScore: projection.ConfidenceScore)));
    }

    public async Task<Result<StackedTimeSeries>> GetStackedTimeSeriesAsync(
        string granularity = "Daily",
        int periodsBack = 180,
        int? streamType = null,
        string? providerId = null,
        CancellationToken ct = default)
    {
        var query = new GetStackedTimeSeriesQuery(granularity, periodsBack, StreamType: streamType, ProviderId: providerId);
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
        string? providerId = null,
        CancellationToken ct = default)
    {
        var query = new GetStreamTrendsQuery(comparisonType, StreamType: streamType, ProviderId: providerId);
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
