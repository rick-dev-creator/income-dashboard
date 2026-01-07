namespace Analytics.Contracts.DTOs;

public sealed record DailyRateDto(
    decimal AverageDailyUsd,
    decimal MedianDailyUsd,
    int DaysAnalyzed,
    decimal StandardDeviation,
    decimal CoefficientOfVariation,
    DateOnly FromDate,
    DateOnly ToDate);
