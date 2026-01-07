using Analytics.Contracts.DTOs;
using FluentResults;

namespace Analytics.Contracts.Queries;

public sealed record GetStreamTrendsQuery(string ComparisonType = "MoM");

public interface IGetStreamTrendsHandler
{
    Task<Result<StreamTrendsDto>> HandleAsync(GetStreamTrendsQuery query, CancellationToken ct = default);
}
