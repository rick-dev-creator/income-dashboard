namespace Analytics.Contracts.DTOs;

public sealed record MonteCarloResultDto(
    int SimulationCount,
    int MonthsAhead,
    decimal GoalAmount,
    decimal GoalProbability,
    MonteCarloPercentilesDto Percentiles,
    IReadOnlyList<MonteCarloDistributionBucketDto> Distribution,
    IReadOnlyList<MonteCarloMonthlyDto> MonthlyProjections,
    MonteCarloInputsDto Inputs);

public sealed record MonteCarloPercentilesDto(
    decimal P10,
    decimal P25,
    decimal P50,
    decimal P75,
    decimal P90,
    decimal Mean,
    decimal StdDev);

public sealed record MonteCarloDistributionBucketDto(
    decimal RangeStart,
    decimal RangeEnd,
    string Label,
    int Count,
    decimal Percentage);

public sealed record MonteCarloMonthlyDto(
    DateOnly Month,
    decimal P10,
    decimal P25,
    decimal P50,
    decimal P75,
    decimal P90);

public sealed record MonteCarloInputsDto(
    decimal FixedMonthlyIncome,
    decimal VariableMonthlyIncome,
    decimal VariableVolatility,
    decimal MonthlyGrowthRate,
    int StreamCount,
    int FixedStreamCount,
    int VariableStreamCount);
