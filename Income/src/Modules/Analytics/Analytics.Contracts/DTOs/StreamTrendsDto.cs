namespace Analytics.Contracts.DTOs;

public sealed record StreamTrendsDto(
    IReadOnlyList<StreamTrendItemDto> Streams,
    int GrowingCount,
    int DecliningCount,
    int StableCount);

public sealed record StreamTrendItemDto(
    string StreamId,
    string StreamName,
    string Category,
    string ProviderName,
    decimal CurrentPeriodUsd,
    decimal PreviousPeriodUsd,
    decimal ChangeUsd,
    decimal ChangePercentage,
    string Direction);
