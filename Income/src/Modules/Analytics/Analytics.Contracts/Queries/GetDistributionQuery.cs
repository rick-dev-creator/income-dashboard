using Analytics.Contracts.DTOs;
using FluentResults;

namespace Analytics.Contracts.Queries;

public sealed record GetDistributionQuery(
    string GroupBy,
    DateOnly? StartDate = null,
    DateOnly? EndDate = null,
    int? StreamType = null,
    string? ProviderId = null);

public interface IGetDistributionHandler
{
    Task<Result<DistributionDto>> HandleAsync(GetDistributionQuery query, CancellationToken ct = default);
}
