using Analytics.Contracts.DTOs;
using FluentResults;

namespace Analytics.Contracts.Queries;

public sealed record GetPortfolioSummaryQuery(
    DateOnly? StartDate = null,
    DateOnly? EndDate = null);

public interface IGetPortfolioSummaryHandler
{
    Task<Result<PortfolioSummaryDto>> HandleAsync(GetPortfolioSummaryQuery query, CancellationToken ct = default);
}
