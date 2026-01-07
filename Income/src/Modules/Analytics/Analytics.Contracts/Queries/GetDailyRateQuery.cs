using Analytics.Contracts.DTOs;
using FluentResults;

namespace Analytics.Contracts.Queries;

public sealed record GetDailyRateQuery(int DaysBack = 30);

public interface IGetDailyRateHandler
{
    Task<Result<DailyRateDto>> HandleAsync(GetDailyRateQuery query, CancellationToken ct = default);
}
