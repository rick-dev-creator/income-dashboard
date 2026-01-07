using Income.Infrastructure.Persistence;
using Income.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Income.Infrastructure.Seeding;

internal sealed class SeedDataGenerator(IDbContextFactory<IncomeDbContext> dbContextFactory) : ISeedDataGenerator
{
    public async Task<bool> HasDataAsync(CancellationToken ct = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);
        // Check for our specific seeded providers, not just any provider
        return await dbContext.Providers.AnyAsync(p => p.Id == "provider-manual-salary", ct);
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (await HasDataAsync(ct))
            return;

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var providers = CreateProviders();
        await dbContext.Providers.AddRangeAsync(providers, ct);
        await dbContext.SaveChangesAsync(ct);

        var streams = CreateStreams(providers);
        await dbContext.Streams.AddRangeAsync(streams, ct);
        await dbContext.SaveChangesAsync(ct);
    }

    private static List<ProviderEntity> CreateProviders()
    {
        return
        [
            new ProviderEntity
            {
                Id = "provider-manual-salary",
                Name = "[Seed] Manual Entry",
                Type = 3, // ProviderType.Manual
                DefaultCurrency = "USD",
                SyncFrequency = 3, // SyncFrequency.Manual
                ConfigSchema = null
            },
            new ProviderEntity
            {
                Id = "provider-blofin",
                Name = "[Seed] Blofin",
                Type = 0, // ProviderType.Exchange
                DefaultCurrency = "USDT",
                SyncFrequency = 1, // SyncFrequency.Hourly
                ConfigSchema = """{"properties":{"apiKey":{"type":"string"},"apiSecret":{"type":"string"},"passphrase":{"type":"string"}}}"""
            },
            new ProviderEntity
            {
                Id = "provider-patreon",
                Name = "[Seed] Patreon",
                Type = 1, // ProviderType.Creator
                DefaultCurrency = "USD",
                SyncFrequency = 2, // SyncFrequency.Daily
                ConfigSchema = """{"properties":{"accessToken":{"type":"string"}}}"""
            }
        ];
    }

    private static List<StreamEntity> CreateStreams(List<ProviderEntity> providers)
    {
        var manualProvider = providers.First(p => p.Id == "provider-manual-salary");
        var blofinProvider = providers.First(p => p.Id == "provider-blofin");
        var patreonProvider = providers.First(p => p.Id == "provider-patreon");

        var streams = new List<StreamEntity>();
        var now = DateTime.UtcNow;

        // Salary stream - Monthly recurring $5,000 USD
        var salaryStream = new StreamEntity
        {
            Id = "stream-salary-main",
            ProviderId = manualProvider.Id,
            Name = "Main Job Salary",
            Category = "Salary",
            OriginalCurrency = "USD",
            IsFixed = true,
            FixedPeriod = "Monthly",
            EncryptedCredentials = null,
            SyncState = 0, // Active
            LastSuccessAt = now,
            LastAttemptAt = now,
            LastError = null,
            NextScheduledAt = null,
            CreatedAt = now.AddMonths(-6),
            Snapshots = GenerateMonthlySalarySnapshots(5000m, 6)
        };
        streams.Add(salaryStream);

        // Freelance salary - Monthly recurring $2,000 USD
        var freelanceStream = new StreamEntity
        {
            Id = "stream-salary-freelance",
            ProviderId = manualProvider.Id,
            Name = "Freelance Contract",
            Category = "Salary",
            OriginalCurrency = "USD",
            IsFixed = true,
            FixedPeriod = "Monthly",
            EncryptedCredentials = null,
            SyncState = 0,
            LastSuccessAt = now,
            LastAttemptAt = now,
            LastError = null,
            NextScheduledAt = null,
            CreatedAt = now.AddMonths(-4),
            Snapshots = GenerateMonthlySalarySnapshots(2000m, 4)
        };
        streams.Add(freelanceStream);

        // Trading stream - Variable daily income in USDT
        var tradingStream = new StreamEntity
        {
            Id = "stream-trading-blofin",
            ProviderId = blofinProvider.Id,
            Name = "Blofin Copy Trading",
            Category = "Trading",
            OriginalCurrency = "USDT",
            IsFixed = false,
            FixedPeriod = null,
            EncryptedCredentials = null, // Would be encrypted in real scenario
            SyncState = 0,
            LastSuccessAt = now,
            LastAttemptAt = now,
            LastError = null,
            NextScheduledAt = now.AddHours(1),
            CreatedAt = now.AddMonths(-3),
            Snapshots = GenerateTradingSnapshots(3)
        };
        streams.Add(tradingStream);

        // Patreon stream - Monthly recurring variable
        var patreonStream = new StreamEntity
        {
            Id = "stream-patreon-creator",
            ProviderId = patreonProvider.Id,
            Name = "Patreon Subscriptions",
            Category = "Subscription",
            OriginalCurrency = "USD",
            IsFixed = false,
            FixedPeriod = null,
            EncryptedCredentials = null,
            SyncState = 0,
            LastSuccessAt = now,
            LastAttemptAt = now,
            LastError = null,
            NextScheduledAt = now.AddDays(1),
            CreatedAt = now.AddMonths(-5),
            Snapshots = GeneratePatreonSnapshots(5)
        };
        streams.Add(patreonStream);

        // Referral stream - Sporadic income
        var referralStream = new StreamEntity
        {
            Id = "stream-referral-various",
            ProviderId = manualProvider.Id,
            Name = "Referral Bonuses",
            Category = "Referral",
            OriginalCurrency = "USD",
            IsFixed = false,
            FixedPeriod = null,
            EncryptedCredentials = null,
            SyncState = 0,
            LastSuccessAt = now,
            LastAttemptAt = now,
            LastError = null,
            NextScheduledAt = null,
            CreatedAt = now.AddMonths(-6),
            Snapshots = GenerateReferralSnapshots(6)
        };
        streams.Add(referralStream);

        return streams;
    }

    private static List<SnapshotEntity> GenerateMonthlySalarySnapshots(decimal baseAmount, int months, decimal annualRaisePercent = 0.03m)
    {
        var snapshots = new List<SnapshotEntity>();
        var now = DateTime.UtcNow;
        var random = new Random(42);

        for (var i = months; i >= 0; i--)
        {
            var date = DateOnly.FromDateTime(now.AddMonths(-i));
            var payDate = new DateOnly(date.Year, date.Month, 15);

            if (payDate <= DateOnly.FromDateTime(now))
            {
                // Calculate raise based on how many months ago (simulating annual raises)
                var monthsFromStart = months - i;
                var yearsFromStart = monthsFromStart / 12.0m;
                var raiseMultiplier = 1 + (annualRaisePercent * yearsFromStart);

                // Add slight monthly variation (+/- 2% for bonuses, overtime, etc.)
                var variation = 1 + (decimal)(random.NextDouble() * 0.04 - 0.02);
                var amount = Math.Round(baseAmount * raiseMultiplier * variation, 2);

                snapshots.Add(new SnapshotEntity
                {
                    Id = $"snapshot-salary-{payDate:yyyyMMdd}-{Guid.NewGuid().ToString()[..8]}",
                    Date = payDate,
                    OriginalAmount = amount,
                    OriginalCurrency = "USD",
                    UsdAmount = amount,
                    ExchangeRate = 1m,
                    RateSource = "Fixed",
                    SnapshotAt = DateTime.SpecifyKind(payDate.ToDateTime(TimeOnly.FromDateTime(now)), DateTimeKind.Utc)
                });
            }
        }

        return snapshots;
    }

    private static List<SnapshotEntity> GenerateTradingSnapshots(int months)
    {
        var snapshots = new List<SnapshotEntity>();
        var now = DateTime.UtcNow;
        var random = new Random(42);

        var startDate = DateOnly.FromDateTime(now.AddMonths(-months));
        var endDate = DateOnly.FromDateTime(now);

        // Trading income: daily P&L with trend and volatility
        var baseDailyIncome = 50m; // Base expected daily income
        var volatility = 80m; // Daily volatility
        var monthlyGrowthRate = 0.08m; // 8% monthly improvement in trading skills

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            // Skip weekends (markets closed for some assets)
            if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
            {
                if (random.NextDouble() > 0.3) continue; // 70% chance of no trading on weekends
            }

            var monthsElapsed = (date.DayNumber - startDate.DayNumber) / 30.0m;
            var skillGrowth = 1 + (monthlyGrowthRate * monthsElapsed);

            // Daily P&L: base income with growth + random variation
            // Some days are losses, some are big wins
            var dailyFactor = (decimal)(random.NextDouble() * 2 - 0.5); // -0.5 to 1.5
            var dailyPnL = Math.Round((baseDailyIncome * skillGrowth * dailyFactor) +
                                      (decimal)(random.NextGaussian() * (double)volatility), 2);

            // Only record positive income days (losses are absorbed by capital)
            if (dailyPnL > 0)
            {
                snapshots.Add(new SnapshotEntity
                {
                    Id = $"snapshot-trading-{date:yyyyMMdd}",
                    Date = date,
                    OriginalAmount = dailyPnL,
                    OriginalCurrency = "USDT",
                    UsdAmount = Math.Round(dailyPnL * 0.9998m, 2),
                    ExchangeRate = 0.9998m,
                    RateSource = "CoinGecko",
                    SnapshotAt = DateTime.SpecifyKind(date.ToDateTime(TimeOnly.FromDateTime(now)), DateTimeKind.Utc)
                });
            }
        }

        return snapshots;
    }

    private static List<SnapshotEntity> GeneratePatreonSnapshots(int months)
    {
        var snapshots = new List<SnapshotEntity>();
        var now = DateTime.UtcNow;
        var random = new Random(123);

        var baseSubscribers = 30; // Starting subscribers
        var monthlyGrowthRate = 0.15m; // 15% monthly subscriber growth
        var avgRevenuePerUser = 5.5m; // $5.50 average per patron

        for (var i = months; i >= 0; i--)
        {
            var date = DateOnly.FromDateTime(now.AddMonths(-i));
            var payDate = new DateOnly(date.Year, date.Month, 1);

            if (payDate <= DateOnly.FromDateTime(now))
            {
                var monthsElapsed = months - i;

                // Exponential subscriber growth with some randomness
                var growthMultiplier = (decimal)Math.Pow(1 + (double)monthlyGrowthRate, monthsElapsed);
                var subscribers = (int)(baseSubscribers * growthMultiplier * (decimal)(0.9 + random.NextDouble() * 0.2));

                // Revenue = subscribers * ARPU with seasonal variation
                var seasonalFactor = 1 + 0.1m * (decimal)Math.Sin(monthsElapsed * Math.PI / 6); // Seasonal wave
                var amount = Math.Round(subscribers * avgRevenuePerUser * seasonalFactor, 2);

                snapshots.Add(new SnapshotEntity
                {
                    Id = $"snapshot-patreon-{payDate:yyyyMMdd}",
                    Date = payDate,
                    OriginalAmount = amount,
                    OriginalCurrency = "USD",
                    UsdAmount = amount,
                    ExchangeRate = 1m,
                    RateSource = "Patreon API",
                    SnapshotAt = DateTime.SpecifyKind(payDate.ToDateTime(TimeOnly.FromDateTime(now)), DateTimeKind.Utc)
                });
            }
        }

        return snapshots;
    }

    private static List<SnapshotEntity> GenerateReferralSnapshots(int months)
    {
        var snapshots = new List<SnapshotEntity>();
        var now = DateTime.UtcNow;
        var random = new Random(456);

        var startDate = DateOnly.FromDateTime(now.AddMonths(-months));
        var endDate = DateOnly.FromDateTime(now);

        // Referral frequency increases over time as network grows
        var baseChance = 0.08m; // 8% base daily chance
        var growthFactor = 0.02m; // Increases 2% per month

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            var monthsElapsed = (date.DayNumber - startDate.DayNumber) / 30.0m;
            var currentChance = (double)(baseChance + growthFactor * monthsElapsed);

            if (random.NextDouble() < currentChance)
            {
                // Referral amounts: mix of small and occasional large bonuses
                var isLargeBonus = random.NextDouble() > 0.85; // 15% chance of large bonus
                var amount = isLargeBonus
                    ? (decimal)(random.NextDouble() * 200 + 100) // $100-$300 large bonus
                    : (decimal)(random.NextDouble() * 50 + 15);   // $15-$65 regular referral

                snapshots.Add(new SnapshotEntity
                {
                    Id = $"snapshot-referral-{date:yyyyMMdd}",
                    Date = date,
                    OriginalAmount = Math.Round(amount, 2),
                    OriginalCurrency = "USD",
                    UsdAmount = Math.Round(amount, 2),
                    ExchangeRate = 1m,
                    RateSource = "Manual",
                    SnapshotAt = DateTime.SpecifyKind(date.ToDateTime(TimeOnly.FromDateTime(now)), DateTimeKind.Utc)
                });
            }
        }

        return snapshots;
    }
}

// Extension method for Gaussian random numbers
internal static class RandomExtensions
{
    public static double NextGaussian(this Random random, double mean = 0, double stdDev = 1)
    {
        var u1 = 1.0 - random.NextDouble();
        var u2 = 1.0 - random.NextDouble();
        var randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        return mean + stdDev * randStdNormal;
    }
}
