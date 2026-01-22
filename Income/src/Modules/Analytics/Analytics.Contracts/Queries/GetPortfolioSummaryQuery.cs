using Analytics.Contracts.DTOs;
using FluentResults;

namespace Analytics.Contracts.Queries;

/// <summary>
/// Query to get portfolio/stream summary.
/// </summary>
/// <param name="StartDate">Optional start date filter</param>
/// <param name="EndDate">Optional end date filter</param>
/// <param name="StreamType">Filter by stream type: 0=Income, 1=Outcome, null=Both</param>
/// <param name="ProviderId">Filter by provider ID</param>
public sealed record GetPortfolioSummaryQuery(
    DateOnly? StartDate = null,
    DateOnly? EndDate = null,
    int? StreamType = null,
    string? ProviderId = null);

public interface IGetPortfolioSummaryHandler
{
    Task<Result<PortfolioSummaryDto>> HandleAsync(GetPortfolioSummaryQuery query, CancellationToken ct = default);
}
