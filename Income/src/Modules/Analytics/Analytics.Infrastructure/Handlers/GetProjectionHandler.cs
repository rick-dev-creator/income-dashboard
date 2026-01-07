using Analytics.Contracts.DTOs;
using Analytics.Contracts.Queries;
using FluentResults;
using Income.Contracts.DTOs;
using Income.Contracts.Queries;

namespace Analytics.Infrastructure.Handlers;

internal sealed class GetProjectionHandler(
    IGetAllStreamsHandler streamsHandler) : IGetProjectionHandler
{
    public async Task<Result<ProjectionDto>> HandleAsync(GetProjectionQuery query, CancellationToken ct = default)
    {
        var streamsResult = await streamsHandler.HandleAsync(new GetAllStreamsQuery(), ct);
        if (streamsResult.IsFailed)
            return streamsResult.ToResult<ProjectionDto>();

        var streams = streamsResult.Value;
        var now = DateTime.UtcNow;
        var currentMonth = new DateOnly(now.Year, now.Month, 1);

        var fixedMonthly = CalculateFixedMonthlyIncome(streams);
        var (variableMonthly, variableStdDev) = CalculateVariableMonthlyStats(streams);
        var monthlyGrowthRate = CalculateMonthlyGrowthRate(streams);

        var projectedMonthly = fixedMonthly + variableMonthly;
        var projectedAnnual = projectedMonthly * 12;

        var confidenceScore = CalculateConfidenceScore(streams, fixedMonthly, variableMonthly, variableStdDev);

        var projections = new List<ProjectedPointDto>();
        for (var i = 1; i <= query.MonthsAhead; i++)
        {
            var month = currentMonth.AddMonths(i);

            // Apply compound growth to variable income (fixed income stays constant)
            var growthMultiplier = (decimal)Math.Pow(1 + (double)monthlyGrowthRate, i);
            var projectedVariable = variableMonthly * growthMultiplier;
            var projected = fixedMonthly + projectedVariable;

            // Confidence interval widens as we project further into the future
            var uncertaintyMultiplier = 1 + (i * 0.1m); // 10% more uncertainty per month
            var adjustedStdDev = variableStdDev * uncertaintyMultiplier * growthMultiplier;

            var lowerBound = fixedMonthly + Math.Max(0, projectedVariable - adjustedStdDev * 1.5m);
            var upperBound = fixedMonthly + projectedVariable + adjustedStdDev * 1.5m;

            projections.Add(new ProjectedPointDto(
                Month: month,
                ProjectedUsd: Math.Round(projected, 2),
                LowerBoundUsd: Math.Round(lowerBound, 2),
                UpperBoundUsd: Math.Round(upperBound, 2)));
        }

        var projected6MonthTotal = projections.Take(6).Sum(p => p.ProjectedUsd);

        return Result.Ok(new ProjectionDto(
            ProjectedMonthlyIncomeUsd: Math.Round(projectedMonthly, 2),
            ProjectedAnnualIncomeUsd: Math.Round(projectedAnnual, 2),
            Projected6MonthTotalUsd: Math.Round(projected6MonthTotal, 2),
            FixedComponentUsd: Math.Round(fixedMonthly, 2),
            VariableComponentUsd: Math.Round(variableMonthly, 2),
            ConfidenceScore: Math.Round(confidenceScore, 2),
            MonthlyProjections: projections));
    }

    private static decimal CalculateFixedMonthlyIncome(IReadOnlyList<StreamDto> streams)
    {
        decimal total = 0;
        foreach (var stream in streams.Where(s => s.IsFixed))
        {
            var lastSnapshot = stream.Snapshots.MaxBy(s => s.Date);
            if (lastSnapshot is null) continue;

            var multiplier = stream.FixedPeriod?.ToLower() switch
            {
                "daily" => 30m,
                "weekly" => 4.33m,
                "biweekly" => 2.17m,
                "monthly" => 1m,
                "quarterly" => 0.33m,
                "annually" => 0.083m,
                _ => 1m
            };

            total += lastSnapshot.UsdAmount * multiplier;
        }
        return total;
    }

    private static (decimal Average, decimal StdDev) CalculateVariableMonthlyStats(IReadOnlyList<StreamDto> streams)
    {
        var variableStreams = streams.Where(s => !s.IsFixed).ToList();
        if (variableStreams.Count == 0)
            return (0, 0);

        var monthlyTotals = variableStreams
            .SelectMany(s => s.Snapshots)
            .GroupBy(s => new DateOnly(s.Date.Year, s.Date.Month, 1))
            .Select(g => g.Sum(s => s.UsdAmount))
            .ToList();

        if (monthlyTotals.Count == 0)
            return (0, 0);

        var average = monthlyTotals.Average(x => (double)x);
        var variance = monthlyTotals.Average(x => Math.Pow((double)x - average, 2));
        var stdDev = Math.Sqrt(variance);

        return ((decimal)average, (decimal)stdDev);
    }

    private static decimal CalculateConfidenceScore(
        IReadOnlyList<StreamDto> streams,
        decimal fixedMonthly,
        decimal variableMonthly,
        decimal variableStdDev)
    {
        var totalProjected = fixedMonthly + variableMonthly;
        if (totalProjected == 0)
            return 0;

        var fixedRatio = fixedMonthly / totalProjected;

        var variabilityPenalty = variableMonthly > 0 ? variableStdDev / variableMonthly : 0;
        variabilityPenalty = Math.Min(variabilityPenalty, 1);

        var dataQuality = streams.Count > 0
            ? (decimal)streams.Count(s => s.Snapshots.Count >= 3) / streams.Count
            : 0;

        var confidence = (fixedRatio * 0.5m) + ((1 - variabilityPenalty) * 0.3m) + (dataQuality * 0.2m);
        return Math.Min(Math.Max(confidence, 0), 1);
    }

    private static decimal CalculateMonthlyGrowthRate(IReadOnlyList<StreamDto> streams)
    {
        // Get all snapshots grouped by month
        var allSnapshots = streams
            .SelectMany(s => s.Snapshots)
            .GroupBy(s => new DateOnly(s.Date.Year, s.Date.Month, 1))
            .OrderBy(g => g.Key)
            .Select(g => new { Month = g.Key, Total = g.Sum(s => s.UsdAmount) })
            .ToList();

        if (allSnapshots.Count < 2)
            return 0.02m; // Default 2% monthly growth if insufficient data

        // Calculate month-over-month growth rates
        var growthRates = new List<decimal>();
        for (var i = 1; i < allSnapshots.Count; i++)
        {
            var previous = allSnapshots[i - 1].Total;
            var current = allSnapshots[i].Total;

            if (previous > 0)
            {
                var rate = (current - previous) / previous;
                // Cap extreme values to prevent unrealistic projections
                rate = Math.Max(-0.5m, Math.Min(0.5m, rate));
                growthRates.Add(rate);
            }
        }

        if (growthRates.Count == 0)
            return 0.02m;

        // Use weighted average: more recent months have higher weight
        var weightedSum = 0m;
        var weightTotal = 0m;
        for (var i = 0; i < growthRates.Count; i++)
        {
            var weight = i + 1; // Linear weighting: 1, 2, 3, ...
            weightedSum += growthRates[i] * weight;
            weightTotal += weight;
        }

        var weightedAverage = weightedSum / weightTotal;

        // Apply dampening factor to avoid overly optimistic/pessimistic projections
        return weightedAverage * 0.7m;
    }
}
