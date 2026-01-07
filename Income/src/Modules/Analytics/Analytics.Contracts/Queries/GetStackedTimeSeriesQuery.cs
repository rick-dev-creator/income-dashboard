using Analytics.Contracts.DTOs;
using FluentResults;

namespace Analytics.Contracts.Queries;

public sealed record GetStackedTimeSeriesQuery(
    string Granularity = "Daily",
    int PeriodsBack = 180);

public interface IGetStackedTimeSeriesHandler
{
    Task<Result<StackedTimeSeriesDto>> HandleAsync(GetStackedTimeSeriesQuery query, CancellationToken ct = default);
}
