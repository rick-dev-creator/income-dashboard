using FluentResults;

namespace Income.Application.Services.Streams;

public interface IStreamService
{
    Task<Result<IReadOnlyList<StreamListItem>>> GetAllAsync(CancellationToken ct = default);
    Task<Result<StreamDetail>> GetByIdAsync(string streamId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<StreamListItem>>> GetByProviderAsync(string providerId, CancellationToken ct = default);
    Task<Result<StreamDetail>> CreateAsync(CreateStreamRequest request, CancellationToken ct = default);
    Task<Result<StreamDetail>> UpdateAsync(UpdateStreamRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(string streamId, CancellationToken ct = default);
    Task<Result<SnapshotItem>> RecordSnapshotAsync(RecordSnapshotRequest request, CancellationToken ct = default);
    Task<Result> UpdateCredentialsAsync(string streamId, string? credentials, CancellationToken ct = default);
}

// DTOs
public sealed record StreamListItem(
    string Id,
    string Name,
    string ProviderId,
    string Category,
    string OriginalCurrency,
    bool IsFixed,
    string? FixedPeriod,
    bool HasCredentials,
    int SnapshotCount,
    SnapshotItem? LastSnapshot);

public sealed record StreamDetail(
    string Id,
    string Name,
    string ProviderId,
    string Category,
    string OriginalCurrency,
    bool IsFixed,
    string? FixedPeriod,
    bool HasCredentials,
    IReadOnlyList<SnapshotItem> Snapshots);

public sealed record SnapshotItem(
    string Id,
    DateOnly Date,
    decimal OriginalAmount,
    string OriginalCurrency,
    decimal UsdAmount,
    decimal ExchangeRate,
    string RateSource);

public sealed record CreateStreamRequest(
    string ProviderId,
    string Name,
    string Category,
    string OriginalCurrency,
    bool IsFixed,
    string? FixedPeriod,
    string? Credentials = null);

public sealed record UpdateStreamRequest(
    string StreamId,
    string? Name,
    string? Category);

public sealed record RecordSnapshotRequest(
    string StreamId,
    DateOnly Date,
    decimal OriginalAmount,
    string OriginalCurrency,
    decimal UsdAmount,
    decimal ExchangeRate,
    string RateSource);
