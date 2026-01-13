using Analytics.Contracts.DTOs;
using FluentResults;

namespace Analytics.Contracts.Queries;

public sealed record GetTopPerformersQuery(
    int TopN = 10,
    DateOnly? StartDate = null,
    DateOnly? EndDate = null,
    int? StreamType = null);

public interface IGetTopPerformersHandler
{
    Task<Result<TopPerformersDto>> HandleAsync(GetTopPerformersQuery query, CancellationToken ct = default);
}
