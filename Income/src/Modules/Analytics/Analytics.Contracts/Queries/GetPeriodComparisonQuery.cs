using Analytics.Contracts.DTOs;
using FluentResults;

namespace Analytics.Contracts.Queries;

public sealed record GetPeriodComparisonQuery(
    string ComparisonType,
    DateOnly? ReferenceDate = null,
    int? StreamType = null,
    string? ProviderId = null);

public interface IGetPeriodComparisonHandler
{
    Task<Result<PeriodComparisonDto>> HandleAsync(GetPeriodComparisonQuery query, CancellationToken ct = default);
}
