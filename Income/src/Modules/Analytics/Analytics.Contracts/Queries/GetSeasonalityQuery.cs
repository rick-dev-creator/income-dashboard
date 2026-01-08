using Analytics.Contracts.DTOs;
using FluentResults;

namespace Analytics.Contracts.Queries;

public sealed record GetSeasonalityQuery(int MonthsBack = 12);

public interface IGetSeasonalityHandler
{
    Task<Result<SeasonalityDto>> HandleAsync(GetSeasonalityQuery query, CancellationToken ct = default);
}
