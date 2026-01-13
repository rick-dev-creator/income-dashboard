using FluentResults;
using Income.Application.Connectors;

namespace Income.Application.Services.Providers;

public interface IProviderService
{
    Task<Result<IReadOnlyList<ProviderListItem>>> GetAllAsync(CancellationToken ct = default);
    Task<Result<ProviderDetail>> GetByIdAsync(string providerId, CancellationToken ct = default);
    Task<Result<ProviderDetail>> CreateAsync(CreateProviderRequest request, CancellationToken ct = default);
}

// DTOs
public sealed record ProviderListItem(
    string Id,
    string Name,
    string Type,
    string ConnectorKind,
    string DefaultCurrency,
    string SyncFrequency,
    string? ConfigSchema,
    SupportedStreamTypes SupportedStreamTypes)
{
    /// <summary>
    /// Checks if this provider supports the given stream type.
    /// </summary>
    public bool SupportsStreamType(SupportedStreamTypes streamType) =>
        (SupportedStreamTypes & streamType) == streamType;
}

public sealed record ProviderDetail(
    string Id,
    string Name,
    string Type,
    string ConnectorKind,
    string DefaultCurrency,
    string SyncFrequency,
    string? ConfigSchema,
    SupportedStreamTypes SupportedStreamTypes);

public sealed record CreateProviderRequest(
    string Name,
    string Type,
    string ConnectorKind,
    string DefaultCurrency,
    string SyncFrequency,
    string? ConfigSchema = null);
