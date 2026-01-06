namespace Analytics.Contracts.DTOs;

public sealed record TimeSeriesDto(
    IReadOnlyList<TimeSeriesPointDto> Points,
    string Granularity,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal TotalUsd,
    decimal AverageUsd,
    decimal MinUsd,
    decimal MaxUsd);

public sealed record TimeSeriesPointDto(
    DateOnly Date,
    decimal AmountUsd,
    int SnapshotCount);
