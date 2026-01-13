using FluentResults;
using Income.Contracts.DTOs;

namespace Income.Contracts.Queries;

/// <summary>
/// Query to get all streams, optionally filtered by type.
/// </summary>
/// <param name="StreamType">Optional filter: 0=Income, 1=Outcome, null=All</param>
public sealed record GetAllStreamsQuery(int? StreamType = null);

public interface IGetAllStreamsHandler
{
    Task<Result<IReadOnlyList<StreamDto>>> HandleAsync(GetAllStreamsQuery query, CancellationToken ct = default);
}
