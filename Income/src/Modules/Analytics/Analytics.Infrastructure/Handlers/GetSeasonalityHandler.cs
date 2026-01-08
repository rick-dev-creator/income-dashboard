using System.Globalization;
using Analytics.Contracts.DTOs;
using Analytics.Contracts.Queries;
using FluentResults;
using Income.Contracts.Queries;

namespace Analytics.Infrastructure.Handlers;

internal sealed class GetSeasonalityHandler(
    IGetAllStreamsHandler streamsHandler) : IGetSeasonalityHandler
{
    private static readonly string[] DayNames = ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"];

    public async Task<Result<SeasonalityDto>> HandleAsync(GetSeasonalityQuery query, CancellationToken ct = default)
    {
        var streamsResult = await streamsHandler.HandleAsync(new GetAllStreamsQuery(), ct);
        if (streamsResult.IsFailed)
            return streamsResult.ToResult<SeasonalityDto>();

        var streams = streamsResult.Value;
        var now = DateOnly.FromDateTime(DateTime.UtcNow);
        var fromDate = now.AddMonths(-query.MonthsBack);

        var snapshots = streams
            .SelectMany(s => s.Snapshots)
            .Where(s => s.Date >= fromDate && s.Date <= now)
            .ToList();

        if (snapshots.Count == 0)
        {
            return Result.Ok(CreateEmptyResult());
        }

        // Calculate overall daily average for comparison
        var dailyTotals = snapshots
            .GroupBy(s => s.Date)
            .Select(g => g.Sum(s => s.UsdAmount))
            .ToList();
        var overallDailyAverage = dailyTotals.Count > 0 ? dailyTotals.Average(x => (double)x) : 0;

        // Day of Week Analysis
        var dayOfWeekStats = CalculateDayOfWeekStats(snapshots, overallDailyAverage);

        // Month of Year Analysis
        var monthOfYearStats = CalculateMonthOfYearStats(snapshots, overallDailyAverage);

        // Find best/worst insights
        var bestDay = dayOfWeekStats.MaxBy(d => d.AverageUsd);
        var worstDay = dayOfWeekStats.MinBy(d => d.AverageUsd);
        var bestMonth = monthOfYearStats.MaxBy(m => m.AverageUsd);
        var worstMonth = monthOfYearStats.MinBy(m => m.AverageUsd);

        return Result.Ok(new SeasonalityDto(
            DayOfWeekAnalysis: dayOfWeekStats,
            MonthOfYearAnalysis: monthOfYearStats,
            BestDay: bestDay is not null
                ? new SeasonalityInsight(bestDay.DayName, bestDay.AverageUsd, bestDay.PercentageVsAverage)
                : new SeasonalityInsight("N/A", 0, 0),
            WorstDay: worstDay is not null
                ? new SeasonalityInsight(worstDay.DayName, worstDay.AverageUsd, worstDay.PercentageVsAverage)
                : new SeasonalityInsight("N/A", 0, 0),
            BestMonth: bestMonth is not null
                ? new SeasonalityInsight(bestMonth.MonthName, bestMonth.AverageUsd, bestMonth.PercentageVsAverage)
                : new SeasonalityInsight("N/A", 0, 0),
            WorstMonth: worstMonth is not null
                ? new SeasonalityInsight(worstMonth.MonthName, worstMonth.AverageUsd, worstMonth.PercentageVsAverage)
                : new SeasonalityInsight("N/A", 0, 0),
            TotalDaysAnalyzed: dailyTotals.Count));
    }

    private static List<DayOfWeekStats> CalculateDayOfWeekStats(
        List<Income.Contracts.DTOs.SnapshotDto> snapshots,
        double overallDailyAverage)
    {
        // Group by day of week, then by date to get daily totals per day of week
        var byDayOfWeek = snapshots
            .GroupBy(s => s.Date.DayOfWeek)
            .Select(dayGroup =>
            {
                var dayOfWeek = dayGroup.Key;
                var dailyTotals = dayGroup
                    .GroupBy(s => s.Date)
                    .Select(dateGroup => dateGroup.Sum(s => s.UsdAmount))
                    .ToList();

                var average = dailyTotals.Count > 0 ? dailyTotals.Average(x => (double)x) : 0;
                var percentageVsAvg = overallDailyAverage > 0
                    ? ((average - overallDailyAverage) / overallDailyAverage) * 100
                    : 0;

                return new DayOfWeekStats(
                    DayOfWeek: dayOfWeek,
                    DayName: DayNames[(int)dayOfWeek],
                    AverageUsd: Math.Round((decimal)average, 2),
                    TotalUsd: Math.Round(dayGroup.Sum(s => s.UsdAmount), 2),
                    TransactionCount: dayGroup.Count(),
                    PercentageVsAverage: Math.Round((decimal)percentageVsAvg, 1));
            })
            .OrderBy(d => d.DayOfWeek)
            .ToList();

        // Ensure all days are represented
        var result = new List<DayOfWeekStats>();
        for (var i = 0; i < 7; i++)
        {
            var dayOfWeek = (DayOfWeek)i;
            var existing = byDayOfWeek.FirstOrDefault(d => d.DayOfWeek == dayOfWeek);
            result.Add(existing ?? new DayOfWeekStats(dayOfWeek, DayNames[i], 0, 0, 0, 0));
        }

        return result;
    }

    private static List<MonthOfYearStats> CalculateMonthOfYearStats(
        List<Income.Contracts.DTOs.SnapshotDto> snapshots,
        double overallDailyAverage)
    {
        var byMonth = snapshots
            .GroupBy(s => s.Date.Month)
            .Select(monthGroup =>
            {
                var month = monthGroup.Key;
                var dailyTotals = monthGroup
                    .GroupBy(s => s.Date)
                    .Select(dateGroup => dateGroup.Sum(s => s.UsdAmount))
                    .ToList();

                var average = dailyTotals.Count > 0 ? dailyTotals.Average(x => (double)x) : 0;
                var percentageVsAvg = overallDailyAverage > 0
                    ? ((average - overallDailyAverage) / overallDailyAverage) * 100
                    : 0;

                return new MonthOfYearStats(
                    Month: month,
                    MonthName: CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(month),
                    AverageUsd: Math.Round((decimal)average, 2),
                    TotalUsd: Math.Round(monthGroup.Sum(s => s.UsdAmount), 2),
                    TransactionCount: monthGroup.Count(),
                    PercentageVsAverage: Math.Round((decimal)percentageVsAvg, 1));
            })
            .OrderBy(m => m.Month)
            .ToList();

        return byMonth;
    }

    private static SeasonalityDto CreateEmptyResult()
    {
        var emptyDays = Enumerable.Range(0, 7)
            .Select(i => new DayOfWeekStats((DayOfWeek)i, DayNames[i], 0, 0, 0, 0))
            .ToList();

        return new SeasonalityDto(
            DayOfWeekAnalysis: emptyDays,
            MonthOfYearAnalysis: [],
            BestDay: new SeasonalityInsight("N/A", 0, 0),
            WorstDay: new SeasonalityInsight("N/A", 0, 0),
            BestMonth: new SeasonalityInsight("N/A", 0, 0),
            WorstMonth: new SeasonalityInsight("N/A", 0, 0),
            TotalDaysAnalyzed: 0);
    }
}
