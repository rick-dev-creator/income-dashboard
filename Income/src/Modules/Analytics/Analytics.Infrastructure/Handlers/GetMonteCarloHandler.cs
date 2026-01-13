using Analytics.Contracts.DTOs;
using Analytics.Contracts.Queries;
using FluentResults;
using Income.Contracts.DTOs;
using Income.Contracts.Queries;

namespace Analytics.Infrastructure.Handlers;

internal sealed class GetMonteCarloHandler(
    IGetAllStreamsHandler streamsHandler) : IGetMonteCarloHandler
{
    private readonly Random _random = new();

    public async Task<Result<MonteCarloResultDto>> HandleAsync(GetMonteCarloQuery query, CancellationToken ct = default)
    {
        var streamsResult = await streamsHandler.HandleAsync(new GetAllStreamsQuery(), ct);
        if (streamsResult.IsFailed)
            return streamsResult.ToResult<MonteCarloResultDto>();

        var streams = streamsResult.Value;
        var now = DateTime.UtcNow;
        var currentMonth = new DateOnly(now.Year, now.Month, 1);

        // Calculate base statistics from historical data
        var fixedMonthly = CalculateFixedMonthlyIncome(streams);
        var (variableMonthly, variableStdDev) = CalculateVariableMonthlyStats(streams);
        var monthlyGrowthRate = CalculateMonthlyGrowthRate(streams);

        // Run Monte Carlo simulations
        var simulationResults = RunSimulations(
            query.Simulations,
            query.MonthsAhead,
            fixedMonthly,
            variableMonthly,
            variableStdDev,
            monthlyGrowthRate);

        // Calculate percentiles from final outcomes
        var finalOutcomes = simulationResults.Select(s => s.Last()).OrderBy(x => x).ToList();
        var percentiles = CalculatePercentiles(finalOutcomes);

        // Calculate goal probability
        var goalProbability = query.GoalAmount > 0
            ? (decimal)finalOutcomes.Count(x => x >= query.GoalAmount) / finalOutcomes.Count
            : 0;

        // Create distribution buckets for histogram
        var distribution = CreateDistributionBuckets(finalOutcomes, 10);

        // Calculate monthly percentiles across all simulations
        var monthlyProjections = CalculateMonthlyPercentiles(simulationResults, currentMonth);

        // Calculate effective volatility for display (minimum 10% of total income)
        var totalMonthly = fixedMonthly + variableMonthly;
        var minStdDev = totalMonthly * 0.10m;
        var effectiveStdDev = variableStdDev > minStdDev ? variableStdDev : minStdDev;
        if (effectiveStdDev == 0) effectiveStdDev = 100m;

        var displayVolatility = totalMonthly > 0
            ? Math.Round(effectiveStdDev / totalMonthly * 100, 1)
            : 10m;

        var inputs = new MonteCarloInputsDto(
            FixedMonthlyIncome: Math.Round(fixedMonthly, 2),
            VariableMonthlyIncome: Math.Round(variableMonthly, 2),
            VariableVolatility: displayVolatility,
            MonthlyGrowthRate: Math.Round(monthlyGrowthRate * 100, 2),
            StreamCount: streams.Count,
            FixedStreamCount: streams.Count(s => s.IsFixed),
            VariableStreamCount: streams.Count(s => !s.IsFixed));

        return Result.Ok(new MonteCarloResultDto(
            SimulationCount: query.Simulations,
            MonthsAhead: query.MonthsAhead,
            GoalAmount: query.GoalAmount,
            GoalProbability: Math.Round(goalProbability * 100, 1),
            Percentiles: percentiles,
            Distribution: distribution,
            MonthlyProjections: monthlyProjections,
            Inputs: inputs));
    }

    private List<List<decimal>> RunSimulations(
        int simulationCount,
        int monthsAhead,
        decimal fixedMonthly,
        decimal variableMonthly,
        decimal variableStdDev,
        decimal monthlyGrowthRate)
    {
        var results = new List<List<decimal>>(simulationCount);
        var totalMonthly = fixedMonthly + variableMonthly;

        // Ensure minimum volatility (10% of total income)
        var minStdDev = totalMonthly * 0.10m;
        var effectiveStdDev = variableStdDev > minStdDev ? variableStdDev : minStdDev;

        // If still no volatility, use a small base amount
        if (effectiveStdDev == 0)
            effectiveStdDev = 100m;

        for (var sim = 0; sim < simulationCount; sim++)
        {
            var monthlyOutcomes = new List<decimal>(monthsAhead);
            var cumulativeTotal = 0m;

            for (var month = 1; month <= monthsAhead; month++)
            {
                var growthMultiplier = (decimal)Math.Pow(1 + (double)monthlyGrowthRate, month);

                // Fixed income with small variation (+/- 2%)
                var fixedVariation = 1m + (decimal)GenerateNormalRandom() * 0.02m;
                var actualFixed = Math.Max(0, fixedMonthly * fixedVariation);

                // Variable income with larger variation
                var expectedVariable = variableMonthly * growthMultiplier;
                var randomFactor = GenerateNormalRandom();
                var actualVariable = Math.Max(0, expectedVariable + (effectiveStdDev * growthMultiplier * (decimal)randomFactor));

                var monthTotal = actualFixed + actualVariable;
                cumulativeTotal += monthTotal;
                monthlyOutcomes.Add(cumulativeTotal);
            }

            results.Add(monthlyOutcomes);
        }

        return results;
    }

    private double GenerateNormalRandom()
    {
        // Box-Muller transform for normal distribution
        var u1 = 1.0 - _random.NextDouble();
        var u2 = 1.0 - _random.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
    }

    private static MonteCarloPercentilesDto CalculatePercentiles(List<decimal> sortedValues)
    {
        var count = sortedValues.Count;
        if (count == 0)
            return new MonteCarloPercentilesDto(0, 0, 0, 0, 0, 0, 0);

        var mean = sortedValues.Average();
        var variance = sortedValues.Average(x => Math.Pow((double)(x - mean), 2));
        var stdDev = (decimal)Math.Sqrt(variance);

        return new MonteCarloPercentilesDto(
            P10: Math.Round(sortedValues[(int)(count * 0.10)], 2),
            P25: Math.Round(sortedValues[(int)(count * 0.25)], 2),
            P50: Math.Round(sortedValues[(int)(count * 0.50)], 2),
            P75: Math.Round(sortedValues[(int)(count * 0.75)], 2),
            P90: Math.Round(sortedValues[(int)(count * 0.90)], 2),
            Mean: Math.Round(mean, 2),
            StdDev: Math.Round(stdDev, 2));
    }

    private static List<MonteCarloDistributionBucketDto> CreateDistributionBuckets(List<decimal> values, int bucketCount)
    {
        if (values.Count == 0)
            return [];

        var min = values.Min();
        var max = values.Max();
        var range = max - min;
        var bucketSize = range / bucketCount;

        if (bucketSize == 0)
            bucketSize = 1;

        var buckets = new List<MonteCarloDistributionBucketDto>();

        for (var i = 0; i < bucketCount; i++)
        {
            var rangeStart = min + (bucketSize * i);
            var rangeEnd = min + (bucketSize * (i + 1));
            var count = values.Count(v => v >= rangeStart && (i == bucketCount - 1 ? v <= rangeEnd : v < rangeEnd));
            var percentage = (decimal)count / values.Count * 100;

            buckets.Add(new MonteCarloDistributionBucketDto(
                RangeStart: Math.Round(rangeStart, 0),
                RangeEnd: Math.Round(rangeEnd, 0),
                Label: $"${rangeStart / 1000:F0}k-${rangeEnd / 1000:F0}k",
                Count: count,
                Percentage: Math.Round(percentage, 1)));
        }

        return buckets;
    }

    private static List<MonteCarloMonthlyDto> CalculateMonthlyPercentiles(
        List<List<decimal>> simulations,
        DateOnly startMonth)
    {
        if (simulations.Count == 0 || simulations[0].Count == 0)
            return [];

        var monthCount = simulations[0].Count;
        var results = new List<MonteCarloMonthlyDto>();

        for (var month = 0; month < monthCount; month++)
        {
            var monthValues = simulations.Select(s => s[month]).OrderBy(x => x).ToList();
            var count = monthValues.Count;

            results.Add(new MonteCarloMonthlyDto(
                Month: startMonth.AddMonths(month + 1),
                P10: Math.Round(monthValues[(int)(count * 0.10)], 2),
                P25: Math.Round(monthValues[(int)(count * 0.25)], 2),
                P50: Math.Round(monthValues[(int)(count * 0.50)], 2),
                P75: Math.Round(monthValues[(int)(count * 0.75)], 2),
                P90: Math.Round(monthValues[(int)(count * 0.90)], 2)));
        }

        return results;
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

    private static decimal CalculateMonthlyGrowthRate(IReadOnlyList<StreamDto> streams)
    {
        var allSnapshots = streams
            .SelectMany(s => s.Snapshots)
            .GroupBy(s => new DateOnly(s.Date.Year, s.Date.Month, 1))
            .OrderBy(g => g.Key)
            .Select(g => new { Month = g.Key, Total = g.Sum(s => s.UsdAmount) })
            .ToList();

        if (allSnapshots.Count < 2)
            return 0.02m;

        var growthRates = new List<decimal>();
        for (var i = 1; i < allSnapshots.Count; i++)
        {
            var previous = allSnapshots[i - 1].Total;
            var current = allSnapshots[i].Total;

            if (previous > 0)
            {
                var rate = (current - previous) / previous;
                rate = Math.Max(-0.5m, Math.Min(0.5m, rate));
                growthRates.Add(rate);
            }
        }

        if (growthRates.Count == 0)
            return 0.02m;

        var weightedSum = 0m;
        var weightTotal = 0m;
        for (var i = 0; i < growthRates.Count; i++)
        {
            var weight = i + 1;
            weightedSum += growthRates[i] * weight;
            weightTotal += weight;
        }

        return weightedSum / weightTotal * 0.7m;
    }
}
