using Analytics.Contracts.DTOs;
using FluentResults;

namespace Analytics.Contracts.Queries;

public sealed record GetIncomeTimeSeriesQuery(
    DateOnly StartDate,
    DateOnly EndDate,
    string Granularity = "Daily",
    string? StreamId = null,
    string? ProviderId = null,
    string? Category = null);

public interface IGetIncomeTimeSeriesHandler
{
    Task<Result<TimeSeriesDto>> HandleAsync(GetIncomeTimeSeriesQuery query, CancellationToken ct = default);
}
