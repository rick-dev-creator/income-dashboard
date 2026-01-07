namespace Analytics.Contracts.DTOs;

public sealed record ProjectionDto(
    decimal ProjectedMonthlyIncomeUsd,
    decimal ProjectedAnnualIncomeUsd,
    decimal Projected6MonthTotalUsd,
    decimal FixedComponentUsd,
    decimal VariableComponentUsd,
    decimal ConfidenceScore,
    IReadOnlyList<ProjectedPointDto> MonthlyProjections);

public sealed record ProjectedPointDto(
    DateOnly Month,
    decimal ProjectedUsd,
    decimal LowerBoundUsd,
    decimal UpperBoundUsd);
