using Analytics.Contracts.DTOs;
using FluentResults;

namespace Analytics.Contracts.Queries;

public sealed record GetTrendQuery(
    string Period = "Monthly",
    int PeriodsBack = 6,
    string? StreamId = null,
    string? Category = null,
    int? StreamType = null,
    string? ProviderId = null);

public interface IGetTrendHandler
{
    Task<Result<TrendDto>> HandleAsync(GetTrendQuery query, CancellationToken ct = default);
}
