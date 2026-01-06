namespace Analytics.Contracts.DTOs;

public sealed record PeriodComparisonDto(
    PeriodDataDto CurrentPeriod,
    PeriodDataDto PreviousPeriod,
    decimal ChangeUsd,
    decimal ChangePercentage,
    string Trend);

public sealed record PeriodDataDto(
    DateOnly StartDate,
    DateOnly EndDate,
    decimal TotalUsd,
    int SnapshotCount);
