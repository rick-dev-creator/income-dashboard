using FluentResults;

namespace Income.Contracts.Commands;

public sealed record DeleteStreamCommand(string StreamId);

public interface IDeleteStreamHandler
{
    Task<Result> HandleAsync(DeleteStreamCommand command, CancellationToken ct = default);
}
