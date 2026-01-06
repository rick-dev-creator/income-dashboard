using FluentResults;
using Income.Contracts.DTOs;

namespace Income.Contracts.Queries;

public sealed record GetAllStreamsQuery;

public interface IGetAllStreamsHandler
{
    Task<Result<IReadOnlyList<StreamDto>>> HandleAsync(GetAllStreamsQuery query, CancellationToken ct = default);
}
