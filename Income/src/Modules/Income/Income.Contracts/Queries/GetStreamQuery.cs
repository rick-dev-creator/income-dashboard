using FluentResults;
using Income.Contracts.DTOs;

namespace Income.Contracts.Queries;

public sealed record GetStreamQuery(string StreamId);

public interface IGetStreamHandler
{
    Task<Result<StreamDto>> HandleAsync(GetStreamQuery query, CancellationToken ct = default);
}
