using FluentResults;

namespace Analytics.Application.Services;

public interface IDashboardService
{
    Task<Result<DashboardSummary>> GetSummaryAsync(CancellationToken ct = default);
    Task<Result<IncomeTimeSeries>> GetTimeSeriesAsync(DateOnly startDate, DateOnly endDate, string granularity, string? category = null, CancellationToken ct = default);
    Task<Result<IncomeDistribution>> GetDistributionAsync(string groupBy, DateOnly? startDate = null, DateOnly? endDate = null, CancellationToken ct = default);
    Task<Result<IReadOnlyList<TopPerformerItem>>> GetTopPerformersAsync(int topN = 5, CancellationToken ct = default);
    Task<Result<PeriodComparison>> GetPeriodComparisonAsync(string comparisonType, CancellationToken ct = default);
}

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
