namespace Analytics.Contracts.DTOs;

public sealed record TopPerformersDto(
    IReadOnlyList<TopPerformerItemDto> Items,
    decimal TotalUsd);

public sealed record TopPerformerItemDto(
    string StreamId,
    string StreamName,
    string ProviderId,
    string ProviderName,
    string Category,
    decimal TotalUsd,
    decimal Percentage,
    decimal AveragePerSnapshotUsd,
    int SnapshotCount,
    int Rank);
