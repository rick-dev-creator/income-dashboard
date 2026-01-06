using FluentResults;
using Income.Contracts.DTOs;

namespace Income.Contracts.Commands;

public sealed record CreateStreamCommand(
    string ProviderId,
    string Name,
    string Category,
    string OriginalCurrency,
    bool IsFixed,
    string? FixedPeriod,
    string? Credentials = null);

public interface ICreateStreamHandler
{
    Task<Result<StreamDto>> HandleAsync(CreateStreamCommand command, CancellationToken ct = default);
}
