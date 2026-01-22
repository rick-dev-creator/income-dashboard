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
        var streamsResult = await streamsHandler.HandleAsync(new GetAllStreamsQuery(query.StreamType, query.ProviderId), ct);
        if (streamsResult.IsFailed)
            return streamsResult.ToResult<PeriodComparisonDto>();

        var streams = streamsResult.Value;
        var allSnapshots = streams
            .SelectMany(s => s.Snapshots.Select(snap => (s.StreamType, Snapshot: snap)))
            .ToList();

        var referenceDate = query.ReferenceDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var (currentStart, currentEnd, previousStart, previousEnd) = GetPeriodBounds(query.ComparisonType, referenceDate);

        var currentSnapshots = allSnapshots
            .Where(x => x.Snapshot.Date >= currentStart && x.Snapshot.Date <= currentEnd)
            .ToList();

        var previousSnapshots = allSnapshots
            .Where(x => x.Snapshot.Date >= previousStart && x.Snapshot.Date <= previousEnd)
            .ToList();

        // Calculate totals based on mode:
        // - If StreamType filter is applied: sum all amounts
        // - If no filter (Net Flow mode): Income - Outcome
        decimal currentTotal, previousTotal;
        if (query.StreamType.HasValue)
        {
            currentTotal = currentSnapshots.Sum(x => x.Snapshot.UsdAmount);
            previousTotal = previousSnapshots.Sum(x => x.Snapshot.UsdAmount);
        }
        else
        {
            currentTotal = currentSnapshots.Where(x => x.StreamType == 0).Sum(x => x.Snapshot.UsdAmount)
                         - currentSnapshots.Where(x => x.StreamType == 1).Sum(x => x.Snapshot.UsdAmount);
            previousTotal = previousSnapshots.Where(x => x.StreamType == 0).Sum(x => x.Snapshot.UsdAmount)
                          - previousSnapshots.Where(x => x.StreamType == 1).Sum(x => x.Snapshot.UsdAmount);
        }

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
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var isCurrentPeriod = referenceDate.Year == today.Year && referenceDate.Month == today.Month;

        // If reference is in current month, use equivalent periods (partial comparison)
        // If reference is in a past month, use complete months
        return comparisonType.ToLower() switch
        {
            "mom" or "month-over-month" => isCurrentPeriod
                ? GetMonthOverMonthEquivalent(referenceDate)
                : GetMonthOverMonthComplete(referenceDate),
            "yoy" or "year-over-year" => GetYearOverYearEquivalent(referenceDate),
            "wow" or "week-over-week" => GetWeekOverWeekEquivalent(referenceDate),
            "qoq" or "quarter-over-quarter" => GetQuarterOverQuarterEquivalent(referenceDate),
            _ => isCurrentPeriod
                ? GetMonthOverMonthEquivalent(referenceDate)
                : GetMonthOverMonthComplete(referenceDate)
        };
    }

    private static (DateOnly, DateOnly, DateOnly, DateOnly) GetMonthOverMonthComplete(DateOnly reference)
    {
        // Compare complete months (e.g., full December vs full November)
        var currentStart = new DateOnly(reference.Year, reference.Month, 1);
        var currentEnd = currentStart.AddMonths(1).AddDays(-1); // Last day of the month

        var previousStart = currentStart.AddMonths(-1);
        var previousEnd = currentStart.AddDays(-1); // Last day of previous month

        return (currentStart, currentEnd, previousStart, previousEnd);
    }

    private static (DateOnly, DateOnly, DateOnly, DateOnly) GetMonthOverMonthEquivalent(DateOnly reference)
    {
        var currentStart = new DateOnly(reference.Year, reference.Month, 1);
        var currentEnd = reference; // Today, not end of month

        // Previous period: same day range in previous month
        var previousStart = currentStart.AddMonths(-1);
        // Cap at the last day of previous month if current day exceeds it
        var previousMonthLastDay = currentStart.AddDays(-1).Day;
        var previousEndDay = Math.Min(reference.Day, previousMonthLastDay);
        var previousEnd = new DateOnly(previousStart.Year, previousStart.Month, previousEndDay);

        return (currentStart, currentEnd, previousStart, previousEnd);
    }

    private static (DateOnly, DateOnly, DateOnly, DateOnly) GetYearOverYearEquivalent(DateOnly reference)
    {
        var currentStart = new DateOnly(reference.Year, 1, 1);
        var currentEnd = reference; // Today, not end of year

        // Previous year: same date range
        var previousStart = new DateOnly(reference.Year - 1, 1, 1);
        // Handle leap year edge case for Feb 29
        var previousEnd = reference.Month == 2 && reference.Day == 29 && !DateTime.IsLeapYear(reference.Year - 1)
            ? new DateOnly(reference.Year - 1, 2, 28)
            : new DateOnly(reference.Year - 1, reference.Month, reference.Day);

        return (currentStart, currentEnd, previousStart, previousEnd);
    }

    private static (DateOnly, DateOnly, DateOnly, DateOnly) GetWeekOverWeekEquivalent(DateOnly reference)
    {
        var daysToSubtract = (int)reference.DayOfWeek;
        var currentStart = reference.AddDays(-daysToSubtract);
        var currentEnd = reference; // Today, not end of week
        var daysIntoWeek = (int)reference.DayOfWeek;

        // Previous week: same days (e.g., Sun-Wed vs Sun-Wed)
        var previousStart = currentStart.AddDays(-7);
        var previousEnd = previousStart.AddDays(daysIntoWeek);

        return (currentStart, currentEnd, previousStart, previousEnd);
    }

    private static (DateOnly, DateOnly, DateOnly, DateOnly) GetQuarterOverQuarterEquivalent(DateOnly reference)
    {
        var currentQuarter = (reference.Month - 1) / 3;
        var currentStart = new DateOnly(reference.Year, currentQuarter * 3 + 1, 1);
        var currentEnd = reference; // Today, not end of quarter

        // Previous quarter: same relative position
        var previousStart = currentStart.AddMonths(-3);
        // Calculate days into current quarter
        var daysIntoQuarter = reference.DayNumber - currentStart.DayNumber;
        var previousEnd = previousStart.AddDays(daysIntoQuarter);

        return (currentStart, currentEnd, previousStart, previousEnd);
    }
}
