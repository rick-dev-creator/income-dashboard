using FluentResults;

namespace Analytics.Application.Services;

public interface IDashboardService
{
    Task<Result<DashboardSummary>> GetSummaryAsync(int? streamType = null, string? providerId = null, CancellationToken ct = default);
    Task<Result<DashboardKpis>> GetKpisAsync(int? streamType = null, string? providerId = null, CancellationToken ct = default);
    Task<Result<IncomeTimeSeries>> GetTimeSeriesAsync(DateOnly startDate, DateOnly endDate, string granularity, string? category = null, int? streamType = null, string? providerId = null, CancellationToken ct = default);
    Task<Result<StackedTimeSeries>> GetStackedTimeSeriesAsync(string granularity = "Daily", int periodsBack = 180, int? streamType = null, string? providerId = null, CancellationToken ct = default);
    Task<Result<IncomeDistribution>> GetDistributionAsync(string groupBy, DateOnly? startDate = null, DateOnly? endDate = null, int? streamType = null, string? providerId = null, CancellationToken ct = default);
    Task<Result<IReadOnlyList<TopPerformerItem>>> GetTopPerformersAsync(int topN = 5, int? streamType = null, string? providerId = null, CancellationToken ct = default);
    Task<Result<PeriodComparison>> GetPeriodComparisonAsync(string comparisonType, int? streamType = null, string? providerId = null, CancellationToken ct = default);
    Task<Result<StreamHealthSummary>> GetStreamHealthAsync(string comparisonType = "MoM", int? streamType = null, string? providerId = null, CancellationToken ct = default);
}

public sealed record DashboardKpis(
    DailyRate DailyRate,
    TrendIndicator Trend,
    ProjectionSummary Projection);

public sealed record DailyRate(
    decimal AverageDailyUsd,
    decimal MedianDailyUsd,
    int DaysAnalyzed,
    decimal StandardDeviation,
    decimal CoefficientOfVariation);

public sealed record TrendIndicator(
    decimal ChangePercentage,
    string Direction,
    string ComparisonPeriod);

public sealed record ProjectionSummary(
    decimal Projected6MonthTotalUsd,
    decimal ConfidenceScore);

public sealed record DashboardSummary(
    decimal TotalIncomeUsd,
    decimal FixedMonthlyIncomeUsd,
    int ActiveStreamCount,
    int ProviderCount,
    PeriodComparison? MonthOverMonth);

public sealed record IncomeTimeSeries(
    string Granularity,
    IReadOnlyList<TimeSeriesPoint> Points,
    decimal TotalUsd,
    decimal AverageUsd,
    decimal MinUsd,
    decimal MaxUsd);

public sealed record TimeSeriesPoint(DateOnly Date, decimal AmountUsd);

public sealed record IncomeDistribution(
    string GroupBy,
    IReadOnlyList<DistributionItem> Items,
    decimal TotalUsd);

public sealed record DistributionItem(
    string Key,
    string Label,
    decimal AmountUsd,
    decimal Percentage);

public sealed record TopPerformerItem(
    int Rank,
    string StreamId,
    string StreamName,
    string Category,
    string ProviderName,
    decimal TotalUsd,
    decimal Percentage);

public sealed record PeriodComparison(
    string ComparisonType,
    string CurrentPeriod,
    string PreviousPeriod,
    decimal CurrentAmountUsd,
    decimal PreviousAmountUsd,
    decimal ChangeUsd,
    decimal ChangePercentage,
    string Trend);

public sealed record StackedTimeSeries(
    IReadOnlyList<StackedPoint> Points,
    IReadOnlyList<string> StreamNames,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal TotalUsd);

public sealed record StackedPoint(
    DateOnly Date,
    decimal TotalUsd,
    IReadOnlyList<StreamContribution> Streams);

public sealed record StreamContribution(
    string StreamId,
    string StreamName,
    string Category,
    decimal AmountUsd);

public sealed record StreamHealthSummary(
    IReadOnlyList<StreamHealthItem> Streams,
    int GrowingCount,
    int DecliningCount,
    int StableCount);

public sealed record StreamHealthItem(
    string StreamId,
    string StreamName,
    string Category,
    string ProviderName,
    decimal CurrentPeriodUsd,
    decimal PreviousPeriodUsd,
    decimal ChangeUsd,
    decimal ChangePercentage,
    string Direction);
