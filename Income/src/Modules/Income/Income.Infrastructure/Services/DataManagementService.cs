using FluentResults;
using Income.Application.Connectors;
using Income.Application.Services.DataManagement;
using Income.Infrastructure.Persistence;
using Income.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Income.Infrastructure.Services;

internal sealed class DataManagementService(
    IDbContextFactory<IncomeDbContext> dbContextFactory,
    ILogger<DataManagementService> logger) : IDataManagementService
{
    public async Task<Result<DemoDataResult>> CreateDemoDataAsync(CancellationToken ct = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        // Check if demo data already exists
        if (await dbContext.Streams.AnyAsync(s => s.Id.StartsWith("stream-"), ct))
        {
            return Result.Fail<DemoDataResult>("Demo data already exists. Delete existing streams first.");
        }

        // Ensure the recurring income provider exists
        var provider = await dbContext.Providers.FirstOrDefaultAsync(p => p.Id == BuiltInProviders.RecurringIncome, ct);
        if (provider == null)
        {
            provider = CreateBuiltInProvider();
            await dbContext.Providers.AddAsync(provider, ct);
            await dbContext.SaveChangesAsync(ct);
        }

        var streams = CreateDemoStreams(provider);
        await dbContext.Streams.AddRangeAsync(streams, ct);
        await dbContext.SaveChangesAsync(ct);

        var snapshotCount = streams.Sum(s => s.Snapshots.Count);
        logger.LogInformation("Created {StreamCount} demo streams with {SnapshotCount} snapshots", streams.Count, snapshotCount);

        return Result.Ok(new DemoDataResult(streams.Count, snapshotCount));
    }

    public async Task<Result<DeleteDataResult>> DeleteAllStreamsAsync(CancellationToken ct = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var streamCount = await dbContext.Streams.CountAsync(ct);
        var snapshotCount = await dbContext.Snapshots.CountAsync(ct);

        if (streamCount == 0)
        {
            return Result.Ok(new DeleteDataResult(0, 0));
        }

        // Delete all snapshots first (due to foreign key)
        await dbContext.Snapshots.ExecuteDeleteAsync(ct);

        // Delete all streams
        await dbContext.Streams.ExecuteDeleteAsync(ct);

        logger.LogWarning("Deleted {StreamCount} streams and {SnapshotCount} snapshots", streamCount, snapshotCount);

        return Result.Ok(new DeleteDataResult(streamCount, snapshotCount));
    }

    public async Task<Result<DataStatistics>> GetStatisticsAsync(CancellationToken ct = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var streamCount = await dbContext.Streams.CountAsync(ct);
        var snapshotCount = await dbContext.Snapshots.CountAsync(ct);
        var providerCount = await dbContext.Providers.CountAsync(ct);

        return Result.Ok(new DataStatistics(streamCount, snapshotCount, providerCount));
    }

    private static ProviderEntity CreateBuiltInProvider()
    {
        return new ProviderEntity
        {
            Id = BuiltInProviders.RecurringIncome,
            Name = "Recurring Income",
            Type = 3, // ProviderType.Manual
            ConnectorKind = 0, // ConnectorKind.Recurring
            DefaultCurrency = "USD",
            SyncFrequency = 3, // SyncFrequency.Manual
            ConfigSchema = null,
            SupportedStreamTypes = 3 // Both Income and Outcome
        };
    }

    private static List<StreamEntity> CreateDemoStreams(ProviderEntity provider)
    {
        var streams = new List<StreamEntity>();
        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);

        // Test stream - Daily recurring $50 USD (for testing)
        var testStream = new StreamEntity
        {
            Id = "stream-test-daily",
            ProviderId = provider.Id,
            Name = "Daily Test Income",
            Category = "Income",
            OriginalCurrency = "USD",
            IsFixed = true,
            FixedPeriod = "Daily",
            EncryptedCredentials = null,
            SyncState = 0,
            StreamType = 0,
            LinkedIncomeStreamId = null,
            LastSuccessAt = null,
            LastAttemptAt = null,
            LastError = null,
            NextScheduledAt = null,
            CreatedAt = now,
            RecurringAmount = 50m,
            RecurringFrequency = (int)RecurringFrequency.Daily,
            RecurringStartDate = today,
            Snapshots = []
        };
        streams.Add(testStream);

        // Salary stream - Monthly recurring $8,500 USD
        var salaryStream = new StreamEntity
        {
            Id = "stream-salary-main",
            ProviderId = provider.Id,
            Name = "Main Job Salary",
            Category = "Income",
            OriginalCurrency = "USD",
            IsFixed = true,
            FixedPeriod = "Monthly",
            EncryptedCredentials = null,
            SyncState = 0,
            StreamType = 0,
            LinkedIncomeStreamId = null,
            LastSuccessAt = now,
            LastAttemptAt = now,
            LastError = null,
            NextScheduledAt = null,
            CreatedAt = now.AddMonths(-6),
            RecurringAmount = 8500m,
            RecurringFrequency = (int)RecurringFrequency.Monthly,
            RecurringStartDate = DateOnly.FromDateTime(now.AddMonths(-6)).AddDays(14),
            Snapshots = GenerateMonthlySalarySnapshots(8500m, 6, 0.05m)
        };
        streams.Add(salaryStream);

        // Freelance/Consulting - Monthly recurring $3,500 USD
        var freelanceStream = new StreamEntity
        {
            Id = "stream-salary-freelance",
            ProviderId = provider.Id,
            Name = "Consulting Contract",
            Category = "Income",
            OriginalCurrency = "USD",
            IsFixed = true,
            FixedPeriod = "Monthly",
            EncryptedCredentials = null,
            SyncState = 0,
            StreamType = 0,
            LinkedIncomeStreamId = null,
            LastSuccessAt = now,
            LastAttemptAt = now,
            LastError = null,
            NextScheduledAt = null,
            CreatedAt = now.AddMonths(-4),
            RecurringAmount = 3500m,
            RecurringFrequency = (int)RecurringFrequency.Monthly,
            RecurringStartDate = DateOnly.FromDateTime(now.AddMonths(-4)).AddDays(0),
            Snapshots = GenerateMonthlySalarySnapshots(3500m, 4, 0.08m)
        };
        streams.Add(freelanceStream);

        // Rent income - Monthly recurring $1,200 USD
        var rentStream = new StreamEntity
        {
            Id = "stream-rent-apartment",
            ProviderId = provider.Id,
            Name = "Apartment Rental",
            Category = "Income",
            OriginalCurrency = "USD",
            IsFixed = true,
            FixedPeriod = "Monthly",
            EncryptedCredentials = null,
            SyncState = 0,
            StreamType = 0,
            LinkedIncomeStreamId = null,
            LastSuccessAt = now,
            LastAttemptAt = now,
            LastError = null,
            NextScheduledAt = null,
            CreatedAt = now.AddMonths(-5),
            RecurringAmount = 1200m,
            RecurringFrequency = (int)RecurringFrequency.Monthly,
            RecurringStartDate = DateOnly.FromDateTime(now.AddMonths(-5)).AddDays(0),
            Snapshots = GenerateMonthlySalarySnapshots(1200m, 5, 0.0m)
        };
        streams.Add(rentStream);

        // Variable income - Freelance gigs
        var gigsStream = new StreamEntity
        {
            Id = "stream-freelance-gigs",
            ProviderId = provider.Id,
            Name = "Freelance Gigs",
            Category = "Income",
            OriginalCurrency = "USD",
            IsFixed = false,
            FixedPeriod = null,
            EncryptedCredentials = null,
            SyncState = 0,
            StreamType = 0,
            LinkedIncomeStreamId = null,
            LastSuccessAt = now,
            LastAttemptAt = now,
            LastError = null,
            NextScheduledAt = null,
            CreatedAt = now.AddMonths(-6),
            RecurringAmount = null,
            RecurringFrequency = null,
            RecurringStartDate = null,
            Snapshots = GenerateVariableIncomeSnapshots(2000m, 6, 0.40m)
        };
        streams.Add(gigsStream);

        // OUTCOME STREAMS

        // Rent payment - Monthly $2,200 USD
        var rentExpenseStream = new StreamEntity
        {
            Id = "stream-expense-rent",
            ProviderId = provider.Id,
            Name = "Apartment Rent",
            Category = "Fixed",
            OriginalCurrency = "USD",
            IsFixed = true,
            FixedPeriod = "Monthly",
            EncryptedCredentials = null,
            SyncState = 0,
            StreamType = 1,
            LinkedIncomeStreamId = null,
            LastSuccessAt = now,
            LastAttemptAt = now,
            LastError = null,
            NextScheduledAt = null,
            CreatedAt = now.AddMonths(-6),
            RecurringAmount = 2200m,
            RecurringFrequency = (int)RecurringFrequency.Monthly,
            RecurringStartDate = DateOnly.FromDateTime(now.AddMonths(-6)).AddDays(0),
            Snapshots = GenerateMonthlySalarySnapshots(2200m, 6, 0.0m)
        };
        streams.Add(rentExpenseStream);

        // Utilities - Monthly ~$150 USD
        var utilitiesStream = new StreamEntity
        {
            Id = "stream-expense-utilities",
            ProviderId = provider.Id,
            Name = "Utilities (Electric/Gas/Water)",
            Category = "Fixed",
            OriginalCurrency = "USD",
            IsFixed = true,
            FixedPeriod = "Monthly",
            EncryptedCredentials = null,
            SyncState = 0,
            StreamType = 1,
            LinkedIncomeStreamId = null,
            LastSuccessAt = now,
            LastAttemptAt = now,
            LastError = null,
            NextScheduledAt = null,
            CreatedAt = now.AddMonths(-6),
            RecurringAmount = 150m,
            RecurringFrequency = (int)RecurringFrequency.Monthly,
            RecurringStartDate = DateOnly.FromDateTime(now.AddMonths(-6)).AddDays(15),
            Snapshots = GenerateMonthlySalarySnapshots(150m, 6, 0.15m)
        };
        streams.Add(utilitiesStream);

        // Netflix subscription
        var netflixStream = new StreamEntity
        {
            Id = "stream-expense-netflix",
            ProviderId = provider.Id,
            Name = "Netflix",
            Category = "Fixed",
            OriginalCurrency = "USD",
            IsFixed = true,
            FixedPeriod = "Monthly",
            EncryptedCredentials = null,
            SyncState = 0,
            StreamType = 1,
            LinkedIncomeStreamId = null,
            LastSuccessAt = now,
            LastAttemptAt = now,
            LastError = null,
            NextScheduledAt = null,
            CreatedAt = now.AddMonths(-6),
            RecurringAmount = 15.99m,
            RecurringFrequency = (int)RecurringFrequency.Monthly,
            RecurringStartDate = DateOnly.FromDateTime(now.AddMonths(-6)).AddDays(5),
            Snapshots = GenerateMonthlySalarySnapshots(15.99m, 6, 0.0m)
        };
        streams.Add(netflixStream);

        // Gym membership
        var gymStream = new StreamEntity
        {
            Id = "stream-expense-gym",
            ProviderId = provider.Id,
            Name = "Gym Membership",
            Category = "Fixed",
            OriginalCurrency = "USD",
            IsFixed = true,
            FixedPeriod = "Monthly",
            EncryptedCredentials = null,
            SyncState = 0,
            StreamType = 1,
            LinkedIncomeStreamId = null,
            LastSuccessAt = now,
            LastAttemptAt = now,
            LastError = null,
            NextScheduledAt = null,
            CreatedAt = now.AddMonths(-3),
            RecurringAmount = 50m,
            RecurringFrequency = (int)RecurringFrequency.Monthly,
            RecurringStartDate = DateOnly.FromDateTime(now.AddMonths(-3)).AddDays(1),
            Snapshots = GenerateMonthlySalarySnapshots(50m, 3, 0.0m)
        };
        streams.Add(gymStream);

        return streams;
    }

    private static List<SnapshotEntity> GenerateVariableIncomeSnapshots(decimal avgAmount, int months, decimal volatility)
    {
        var snapshots = new List<SnapshotEntity>();
        var now = DateTime.UtcNow;
        var random = new Random(123);

        for (var i = months; i >= 0; i--)
        {
            var monthDate = DateOnly.FromDateTime(now.AddMonths(-i));
            var paymentsThisMonth = random.Next(1, 5);

            for (var p = 0; p < paymentsThisMonth; p++)
            {
                var day = random.Next(1, 28);
                var payDate = new DateOnly(monthDate.Year, monthDate.Month, day);

                if (payDate <= DateOnly.FromDateTime(now))
                {
                    var variation = 1 + (decimal)((random.NextDouble() * 2 - 1) * (double)volatility);
                    var amount = Math.Round(avgAmount / paymentsThisMonth * variation, 2);
                    amount = Math.Max(100m, amount);

                    snapshots.Add(new SnapshotEntity
                    {
                        Id = $"snapshot-gig-{payDate:yyyyMMdd}-{Guid.NewGuid().ToString()[..8]}",
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
        }

        return snapshots;
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
                var monthsFromStart = months - i;
                var yearsFromStart = monthsFromStart / 12.0m;
                var raiseMultiplier = 1 + (annualRaisePercent * yearsFromStart);
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
}
