using Analytics.Contracts.DTOs;
using FluentResults;

namespace Analytics.Contracts.Queries;

public sealed record GetMonteCarloQuery(
    int Simulations = 10000,
    int MonthsAhead = 6,
    decimal GoalAmount = 0);

public interface IGetMonteCarloHandler
{
    Task<Result<MonteCarloResultDto>> HandleAsync(GetMonteCarloQuery query, CancellationToken ct = default);
}
