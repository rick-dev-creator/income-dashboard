using Analytics.Contracts.DTOs;
using FluentResults;

namespace Analytics.Contracts.Queries;

/// <summary>
/// Query to get stream trends.
/// </summary>
/// <param name="ComparisonType">Comparison type: MoM (month over month), etc.</param>
/// <param name="StreamType">Filter by stream type: 0=Income, 1=Outcome, null=Both</param>
public sealed record GetStreamTrendsQuery(string ComparisonType = "MoM", int? StreamType = null);

public interface IGetStreamTrendsHandler
{
    Task<Result<StreamTrendsDto>> HandleAsync(GetStreamTrendsQuery query, CancellationToken ct = default);
}
