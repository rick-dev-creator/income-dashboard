using Analytics.Contracts.DTOs;
using FluentResults;

namespace Analytics.Contracts.Queries;

/// <summary>
/// Query to get stacked time series data by stream.
/// </summary>
/// <param name="Granularity">Aggregation granularity: Daily, Weekly, Monthly</param>
/// <param name="PeriodsBack">Number of periods to look back</param>
/// <param name="StreamType">Filter by stream type: 0=Income, 1=Outcome, null=Both</param>
public sealed record GetStackedTimeSeriesQuery(
    string Granularity = "Daily",
    int PeriodsBack = 180,
    int? StreamType = null);

public interface IGetStackedTimeSeriesHandler
{
    Task<Result<StackedTimeSeriesDto>> HandleAsync(GetStackedTimeSeriesQuery query, CancellationToken ct = default);
}
