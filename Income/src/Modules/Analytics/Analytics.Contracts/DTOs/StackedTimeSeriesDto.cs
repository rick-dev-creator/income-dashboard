namespace Analytics.Contracts.DTOs;

public sealed record StackedTimeSeriesDto(
    IReadOnlyList<StackedPointDto> Points,
    IReadOnlyList<string> StreamNames,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal TotalUsd);

public sealed record StackedPointDto(
    DateOnly Date,
    decimal TotalUsd,
    IReadOnlyList<StreamContributionDto> Streams);

public sealed record StreamContributionDto(
    string StreamId,
    string StreamName,
    string Category,
    decimal AmountUsd);
