using Analytics.Contracts.DTOs;
using FluentResults;

namespace Analytics.Contracts.Queries;

/// <summary>
/// Query to get the daily income/expense rate.
/// </summary>
/// <param name="DaysBack">Number of days to look back for calculation</param>
/// <param name="StreamType">Filter by stream type: 0=Income, 1=Outcome, null=Both</param>
public sealed record GetDailyRateQuery(int DaysBack = 30, int? StreamType = null);

public interface IGetDailyRateHandler
{
    Task<Result<DailyRateDto>> HandleAsync(GetDailyRateQuery query, CancellationToken ct = default);
}
