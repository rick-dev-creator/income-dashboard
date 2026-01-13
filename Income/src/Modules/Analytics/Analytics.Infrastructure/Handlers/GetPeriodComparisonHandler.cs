using Analytics.Contracts.DTOs;
using Analytics.Contracts.Queries;
using FluentResults;
using Income.Contracts.Queries;

namespace Analytics.Infrastructure.Handlers;

internal sealed class GetPeriodComparisonHandler(
    IGetAllStreamsHandler streamsHandler) : IGetPeriodComparisonHandler
{
    public async Task<Result<PeriodComparisonDto>> HandleAsync(GetPeriodComparisonQuery query, CancellationToken ct = default)
    {
        var streamsResult = await streamsHandler.HandleAsync(new GetAllStreamsQuery(query.StreamType), ct);
        if (streamsResult.IsFailed)
            return streamsResult.ToResult<PeriodComparisonDto>();

        var streams = streamsResult.Value;
        var allSnapshots = streams.SelectMany(s => s.Snapshots).ToList();

        var referenceDate = query.ReferenceDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var (currentStart, currentEnd, previousStart, previousEnd) = GetPeriodBounds(query.ComparisonType, referenceDate);

        var currentSnapshots = allSnapshots
            .Where(s => s.Date >= currentStart && s.Date <= currentEnd)
            .ToList();

        var previousSnapshots = allSnapshots
            .Where(s => s.Date >= previousStart && s.Date <= previousEnd)
            .ToList();

        var currentTotal = currentSnapshots.Sum(s => s.UsdAmount);
        var previousTotal = previousSnapshots.Sum(s => s.UsdAmount);

        var changeUsd = currentTotal - previousTotal;
        var changePercentage = previousTotal != 0 ? (changeUsd / previousTotal) * 100 : 0;

        var trend = changeUsd switch
        {
            > 0 => "Up",
            < 0 => "Down",
            _ => "Flat"
        };

        return Result.Ok(new PeriodComparisonDto(
            CurrentPeriod: new PeriodDataDto(currentStart, currentEnd, currentTotal, currentSnapshots.Count),
            PreviousPeriod: new PeriodDataDto(previousStart, previousEnd, previousTotal, previousSnapshots.Count),
            ChangeUsd: changeUsd,
            ChangePercentage: Math.Round(changePercentage, 2),
            Trend: trend));
    }

    private static (DateOnly CurrentStart, DateOnly CurrentEnd, DateOnly PreviousStart, DateOnly PreviousEnd) GetPeriodBounds(
        string comparisonType, DateOnly referenceDate)
    {
        return comparisonType.ToLower() switch
        {
            "mom" or "month-over-month" => GetMonthOverMonth(referenceDate),
            "yoy" or "year-over-year" => GetYearOverYear(referenceDate),
            "wow" or "week-over-week" => GetWeekOverWeek(referenceDate),
            "qoq" or "quarter-over-quarter" => GetQuarterOverQuarter(referenceDate),
            _ => GetMonthOverMonth(referenceDate)
        };
    }

    private static (DateOnly, DateOnly, DateOnly, DateOnly) GetMonthOverMonth(DateOnly reference)
    {
        var currentStart = new DateOnly(reference.Year, reference.Month, 1);
        var currentEnd = currentStart.AddMonths(1).AddDays(-1);
        var previousStart = currentStart.AddMonths(-1);
        var previousEnd = currentStart.AddDays(-1);
        return (currentStart, currentEnd, previousStart, previousEnd);
    }

    private static (DateOnly, DateOnly, DateOnly, DateOnly) GetYearOverYear(DateOnly reference)
    {
        var currentStart = new DateOnly(reference.Year, 1, 1);
        var currentEnd = new DateOnly(reference.Year, 12, 31);
        var previousStart = new DateOnly(reference.Year - 1, 1, 1);
        var previousEnd = new DateOnly(reference.Year - 1, 12, 31);
        return (currentStart, currentEnd, previousStart, previousEnd);
    }

    private static (DateOnly, DateOnly, DateOnly, DateOnly) GetWeekOverWeek(DateOnly reference)
    {
        var daysToSubtract = (int)reference.DayOfWeek;
        var currentStart = reference.AddDays(-daysToSubtract);
        var currentEnd = currentStart.AddDays(6);
        var previousStart = currentStart.AddDays(-7);
        var previousEnd = currentStart.AddDays(-1);
        return (currentStart, currentEnd, previousStart, previousEnd);
    }

    private static (DateOnly, DateOnly, DateOnly, DateOnly) GetQuarterOverQuarter(DateOnly reference)
    {
        var currentQuarter = (reference.Month - 1) / 3;
        var currentStart = new DateOnly(reference.Year, currentQuarter * 3 + 1, 1);
        var currentEnd = currentStart.AddMonths(3).AddDays(-1);
        var previousStart = currentStart.AddMonths(-3);
        var previousEnd = currentStart.AddDays(-1);
        return (currentStart, currentEnd, previousStart, previousEnd);
    }
}
