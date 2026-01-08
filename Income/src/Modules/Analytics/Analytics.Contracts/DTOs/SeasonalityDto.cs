namespace Analytics.Contracts.DTOs;

public sealed record SeasonalityDto(
    IReadOnlyList<DayOfWeekStats> DayOfWeekAnalysis,
    IReadOnlyList<MonthOfYearStats> MonthOfYearAnalysis,
    SeasonalityInsight BestDay,
    SeasonalityInsight WorstDay,
    SeasonalityInsight BestMonth,
    SeasonalityInsight WorstMonth,
    int TotalDaysAnalyzed);

public sealed record DayOfWeekStats(
    DayOfWeek DayOfWeek,
    string DayName,
    decimal AverageUsd,
    decimal TotalUsd,
    int TransactionCount,
    decimal PercentageVsAverage);

public sealed record MonthOfYearStats(
    int Month,
    string MonthName,
    decimal AverageUsd,
    decimal TotalUsd,
    int TransactionCount,
    decimal PercentageVsAverage);

public sealed record SeasonalityInsight(
    string Name,
    decimal AverageUsd,
    decimal PercentageVsAverage);
