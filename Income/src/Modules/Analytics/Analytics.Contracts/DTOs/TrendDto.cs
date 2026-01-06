namespace Analytics.Contracts.DTOs;

public sealed record TrendDto(
    string Period,
    decimal GrowthRatePercentage,
    string Direction,
    decimal AverageGrowthPerPeriodUsd,
    IReadOnlyList<TrendPointDto> Points);

public sealed record TrendPointDto(
    DateOnly Date,
    decimal AmountUsd,
    decimal CumulativeUsd,
    decimal GrowthFromPreviousUsd,
    decimal GrowthFromPreviousPercentage);
