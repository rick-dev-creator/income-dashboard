using FluentResults;

namespace Analytics.Application.Services;

public interface IAnalyticsService
{
    Task<Result<AnalyticsTrend>> GetTrendAsync(string period, int periodsBack, CancellationToken ct = default);
    Task<Result<IncomeProjection>> GetProjectionAsync(int monthsAhead, CancellationToken ct = default);
    Task<Result<MonteCarloResult>> GetMonteCarloAsync(int simulations, int monthsAhead, decimal goalAmount, CancellationToken ct = default);
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

public sealed record MonteCarloResult(
    int SimulationCount,
    int MonthsAhead,
    decimal GoalAmount,
    decimal GoalProbability,
    MonteCarloPercentiles Percentiles,
    IReadOnlyList<MonteCarloDistributionBucket> Distribution,
    IReadOnlyList<MonteCarloMonthly> MonthlyProjections,
    MonteCarloInputs Inputs);

public sealed record MonteCarloPercentiles(
    decimal P10,
    decimal P25,
    decimal P50,
    decimal P75,
    decimal P90,
    decimal Mean,
    decimal StdDev);

public sealed record MonteCarloDistributionBucket(
    decimal RangeStart,
    decimal RangeEnd,
    string Label,
    int Count,
    decimal Percentage);

public sealed record MonteCarloMonthly(
    DateOnly Month,
    decimal P10,
    decimal P25,
    decimal P50,
    decimal P75,
    decimal P90);

public sealed record MonteCarloInputs(
    decimal FixedMonthlyIncome,
    decimal VariableMonthlyIncome,
    decimal VariableVolatility,
    decimal MonthlyGrowthRate,
    int StreamCount,
    int FixedStreamCount,
    int VariableStreamCount);
