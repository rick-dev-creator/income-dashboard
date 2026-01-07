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

    private static List<SnapshotEntity> GenerateMonthlySalarySnapshots(decimal amount, int months)
    {
        var snapshots = new List<SnapshotEntity>();
        var now = DateTime.UtcNow;

        for (var i = months; i >= 0; i--)
        {
            var date = DateOnly.FromDateTime(now.AddMonths(-i));
            // Salary typically on the 1st or 15th
            var payDate = new DateOnly(date.Year, date.Month, 15);

            if (payDate <= DateOnly.FromDateTime(now))
            {
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
        var random = new Random(42); // Fixed seed for reproducibility

        var startDate = DateOnly.FromDateTime(now.AddMonths(-months));
        var endDate = DateOnly.FromDateTime(now);

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            // Trading results vary daily: -$50 to +$200
            var dailyPnL = (decimal)(random.NextDouble() * 250 - 50);
            // Accumulate balance starting from $1000
            var baseAmount = 1000m + (date.DayNumber - startDate.DayNumber) * 15m; // Slight upward trend
            var amount = Math.Max(0, baseAmount + dailyPnL);

            snapshots.Add(new SnapshotEntity
            {
                Id = $"snapshot-trading-{date:yyyyMMdd}",
                Date = date,
                OriginalAmount = amount,
                OriginalCurrency = "USDT",
                UsdAmount = amount * 0.9998m, // USDT slight deviation
                ExchangeRate = 0.9998m,
                RateSource = "CoinGecko",
                SnapshotAt = DateTime.SpecifyKind(date.ToDateTime(TimeOnly.FromDateTime(now)), DateTimeKind.Utc)
            });
        }

        return snapshots;
    }

    private static List<SnapshotEntity> GeneratePatreonSnapshots(int months)
    {
        var snapshots = new List<SnapshotEntity>();
        var now = DateTime.UtcNow;
        var random = new Random(123);

        for (var i = months; i >= 0; i--)
        {
            var date = DateOnly.FromDateTime(now.AddMonths(-i));
            var payDate = new DateOnly(date.Year, date.Month, 1); // Patreon pays on 1st

            if (payDate <= DateOnly.FromDateTime(now))
            {
                // Growing subscriber base: $150 base + growth
                var subscriberGrowth = (months - i) * 25m;
                var amount = 150m + subscriberGrowth + (decimal)(random.NextDouble() * 50);

                snapshots.Add(new SnapshotEntity
                {
                    Id = $"snapshot-patreon-{payDate:yyyyMMdd}",
                    Date = payDate,
                    OriginalAmount = Math.Round(amount, 2),
                    OriginalCurrency = "USD",
                    UsdAmount = Math.Round(amount, 2),
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

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            // Referrals are sporadic - only some days have income
            if (random.NextDouble() > 0.85) // ~15% chance of referral bonus
            {
                var amount = (decimal)(random.NextDouble() * 100 + 10); // $10-$110

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
