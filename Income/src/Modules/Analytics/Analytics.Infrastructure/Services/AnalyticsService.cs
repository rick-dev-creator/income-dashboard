using Analytics.Application.Services;
using Analytics.Contracts.Queries;
using FluentResults;

namespace Analytics.Infrastructure.Services;

internal sealed class AnalyticsService(
    IGetTrendHandler trendHandler,
    IGetProjectionHandler projectionHandler) : IAnalyticsService
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
}
