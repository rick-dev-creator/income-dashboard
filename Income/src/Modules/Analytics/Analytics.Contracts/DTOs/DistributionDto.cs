namespace Analytics.Contracts.DTOs;

public sealed record DistributionDto(
    IReadOnlyList<DistributionItemDto> Items,
    decimal TotalUsd);

public sealed record DistributionItemDto(
    string Key,
    string Label,
    decimal AmountUsd,
    decimal Percentage,
    int SnapshotCount);
