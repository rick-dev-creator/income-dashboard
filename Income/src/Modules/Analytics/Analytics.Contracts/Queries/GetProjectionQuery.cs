using Analytics.Contracts.DTOs;
using FluentResults;

namespace Analytics.Contracts.Queries;

public sealed record GetProjectionQuery(
    int MonthsAhead = 12,
    int? StreamType = null,
    string? ProviderId = null);

public interface IGetProjectionHandler
{
    Task<Result<ProjectionDto>> HandleAsync(GetProjectionQuery query, CancellationToken ct = default);
}
