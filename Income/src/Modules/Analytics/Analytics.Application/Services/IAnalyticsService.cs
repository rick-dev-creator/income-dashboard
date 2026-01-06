using FluentResults;

namespace Analytics.Application.Services;

public interface IAnalyticsService
{
    Task<Result<AnalyticsTrend>> GetTrendAsync(string period, int periodsBack, CancellationToken ct = default);
    Task<Result<IncomeProjection>> GetProjectionAsync(int monthsAhead, CancellationToken ct = default);
}

public sealed record AnalyticsTrend(
    string Period,
    string Direction,
    decimal GrowthRatePercentage,
    decimal AverageGrowthPerPeriodUsd,
    IReadOnlyList<TrendPoint> Points);

public sealed record TrendPoint(
    DateOnly Date,
    decimal AmountUsd,
    decimal ChangeFromPreviousUsd,
    decimal ChangePercentage);

public sealed record IncomeProjection(
    decimal ProjectedMonthlyIncomeUsd,
    decimal ProjectedAnnualIncomeUsd,
    decimal ConfidenceScore,
    IReadOnlyList<MonthlyProjection> MonthlyProjections);

public sealed record MonthlyProjection(
    DateOnly Month,
    decimal ProjectedAmountUsd,
    decimal LowerBoundUsd,
    decimal UpperBoundUsd);
