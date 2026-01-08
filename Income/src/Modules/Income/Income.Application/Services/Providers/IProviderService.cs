using FluentResults;

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
    string? ConfigSchema);

public sealed record ProviderDetail(
    string Id,
    string Name,
    string Type,
    string ConnectorKind,
    string DefaultCurrency,
    string SyncFrequency,
    string? ConfigSchema);

public sealed record CreateProviderRequest(
    string Name,
    string Type,
    string ConnectorKind,
    string DefaultCurrency,
    string SyncFrequency,
    string? ConfigSchema = null);
