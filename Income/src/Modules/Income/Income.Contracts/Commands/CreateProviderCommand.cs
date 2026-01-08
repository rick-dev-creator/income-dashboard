using FluentResults;
using Income.Contracts.DTOs;

namespace Income.Contracts.Commands;

public sealed record CreateProviderCommand(
    string Name,
    string Type,
    string ConnectorKind,
    string DefaultCurrency,
    string SyncFrequency,
    string? ConfigSchema);

public interface ICreateProviderHandler
{
    Task<Result<ProviderDto>> HandleAsync(CreateProviderCommand command, CancellationToken ct = default);
}
