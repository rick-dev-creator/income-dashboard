namespace Income.Contracts.DTOs;

public sealed record StreamDto(
    string Id,
    string ProviderId,
    string Name,
    string Category,
    string OriginalCurrency,
    bool IsFixed,
    string? FixedPeriod,
    bool HasCredentials,
    SyncStatusDto SyncStatus,
    DateTime CreatedAt,
    IReadOnlyList<SnapshotDto> Snapshots);

public sealed record SyncStatusDto(
    string State,
    DateTime? LastSuccessAt,
    DateTime? LastAttemptAt,
    string? LastError,
    DateTime? NextScheduledAt);

public sealed record SnapshotDto(
    string Id,
    DateOnly Date,
    decimal OriginalAmount,
    string OriginalCurrency,
    decimal UsdAmount,
    decimal ExchangeRate,
    string RateSource,
    DateTime SnapshotAt);
