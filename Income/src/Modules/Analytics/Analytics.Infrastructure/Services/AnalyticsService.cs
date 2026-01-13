using Analytics.Application.Services;
using Analytics.Contracts.Queries;
using FluentResults;

namespace Analytics.Infrastructure.Services;

internal sealed class AnalyticsService(
    IGetTrendHandler trendHandler,
    IGetProjectionHandler projectionHandler,
    IGetMonteCarloHandler monteCarloHandler) : IAnalyticsService
{
    public async Task<Result<AnalyticsTrend>> GetTrendAsync(
        string period,
        int periodsBack,
        CancellationToken ct = default)
    {
        var query = new GetTrendQuery(period, periodsBack);
        var result = await trendHandler.HandleAsync(query, ct);
        if (result.IsFailed)
            return result.ToResult<AnalyticsTrend>();

        var data = result.Value;
        return Result.Ok(new AnalyticsTrend(
            Period: data.Period,
            Direction: data.Direction,
            GrowthRatePercentage: data.GrowthRatePercentage,
            AverageGrowthPerPeriodUsd: data.AverageGrowthPerPeriodUsd,
            Points: data.Points.Select(p => new TrendPoint(
                Date: p.Date,
                AmountUsd: p.AmountUsd,
                ChangeFromPreviousUsd: p.GrowthFromPreviousUsd,
                ChangePercentage: p.GrowthFromPreviousPercentage)).ToList()));
    }

    public async Task<Result<IncomeProjection>> GetProjectionAsync(
        int monthsAhead,
        CancellationToken ct = default)
    {
        var query = new GetProjectionQuery(monthsAhead);
        var result = await projectionHandler.HandleAsync(query, ct);
        if (result.IsFailed)
            return result.ToResult<IncomeProjection>();

        var data = result.Value;
        return Result.Ok(new IncomeProjection(
            ProjectedMonthlyIncomeUsd: data.ProjectedMonthlyIncomeUsd,
            ProjectedAnnualIncomeUsd: data.ProjectedAnnualIncomeUsd,
            ConfidenceScore: data.ConfidenceScore,
            MonthlyProjections: data.MonthlyProjections.Select(p => new MonthlyProjection(
                Month: p.Month,
                ProjectedAmountUsd: p.ProjectedUsd,
                LowerBoundUsd: p.LowerBoundUsd,
                UpperBoundUsd: p.UpperBoundUsd)).ToList()));
    }

    public async Task<Result<MonteCarloResult>> GetMonteCarloAsync(
        int simulations,
        int monthsAhead,
        decimal goalAmount,
        CancellationToken ct = default)
    {
        var query = new GetMonteCarloQuery(simulations, monthsAhead, goalAmount);
        var result = await monteCarloHandler.HandleAsync(query, ct);
        if (result.IsFailed)
            return result.ToResult<MonteCarloResult>();

        var data = result.Value;
        return Result.Ok(new MonteCarloResult(
            SimulationCount: data.SimulationCount,
            MonthsAhead: data.MonthsAhead,
            GoalAmount: data.GoalAmount,
            GoalProbability: data.GoalProbability,
            Percentiles: new MonteCarloPercentiles(
                P10: data.Percentiles.P10,
                P25: data.Percentiles.P25,
                P50: data.Percentiles.P50,
                P75: data.Percentiles.P75,
                P90: data.Percentiles.P90,
                Mean: data.Percentiles.Mean,
                StdDev: data.Percentiles.StdDev),
            Distribution: data.Distribution.Select(d => new MonteCarloDistributionBucket(
                RangeStart: d.RangeStart,
                RangeEnd: d.RangeEnd,
                Label: d.Label,
                Count: d.Count,
                Percentage: d.Percentage)).ToList(),
            MonthlyProjections: data.MonthlyProjections.Select(m => new MonteCarloMonthly(
                Month: m.Month,
                P10: m.P10,
                P25: m.P25,
                P50: m.P50,
                P75: m.P75,
                P90: m.P90)).ToList(),
            Inputs: new MonteCarloInputs(
                FixedMonthlyIncome: data.Inputs.FixedMonthlyIncome,
                VariableMonthlyIncome: data.Inputs.VariableMonthlyIncome,
                VariableVolatility: data.Inputs.VariableVolatility,
                MonthlyGrowthRate: data.Inputs.MonthlyGrowthRate,
                StreamCount: data.Inputs.StreamCount,
                FixedStreamCount: data.Inputs.FixedStreamCount,
                VariableStreamCount: data.Inputs.VariableStreamCount)));
    }
}
