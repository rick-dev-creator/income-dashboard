using FluentResults;

namespace Income.Contracts.Commands;

public sealed record UpdateCredentialsCommand(
    string StreamId,
    string? Credentials);

public interface IUpdateCredentialsHandler
{
    Task<Result> HandleAsync(UpdateCredentialsCommand command, CancellationToken ct = default);
}
