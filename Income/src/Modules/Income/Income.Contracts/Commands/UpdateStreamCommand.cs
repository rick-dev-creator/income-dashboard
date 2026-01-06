using FluentResults;
using Income.Contracts.DTOs;

namespace Income.Contracts.Commands;

public sealed record UpdateStreamCommand(
    string StreamId,
    string? Name,
    string? Category,
    bool? IsFixed = null,
    string? FixedPeriod = null);

public interface IUpdateStreamHandler
{
    Task<Result<StreamDto>> HandleAsync(UpdateStreamCommand command, CancellationToken ct = default);
}
