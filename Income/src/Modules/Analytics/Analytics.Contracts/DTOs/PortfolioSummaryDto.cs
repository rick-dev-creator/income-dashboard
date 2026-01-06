namespace Analytics.Contracts.DTOs;

public sealed record PortfolioSummaryDto(
    decimal TotalIncomeUsd,
    int StreamCount,
    int ActiveStreamCount,
    int ProviderCount,
    decimal AverageIncomePerStreamUsd,
    decimal FixedMonthlyIncomeUsd,
    decimal VariableMonthlyIncomeUsd,
    DateOnly EarliestSnapshotDate,
    DateOnly LatestSnapshotDate);
