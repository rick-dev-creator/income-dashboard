using Income.Application.Connectors;
using Income.Infrastructure.Persistence;
using Income.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Income.Infrastructure.Seeding;

internal sealed class SeedDataGenerator(
    IDbContextFactory<IncomeDbContext> dbContextFactory,
    IConnectorRegistry connectorRegistry,
    ILogger<SeedDataGenerator> logger) : ISeedDataGenerator
{
    public async Task<bool> HasDataAsync(CancellationToken ct = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);
        return await dbContext.Providers.AnyAsync(p => p.Id == BuiltInProviders.RecurringIncome, ct);
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        // Always ensure all connector providers exist
        await EnsureConnectorProvidersExistAsync(dbContext, ct);

        // Only seed demo data if no streams exist yet
        if (!await dbContext.Streams.AnyAsync(ct))
        {
            // Ensure the recurring income provider exists
            var provider = await dbContext.Providers.FirstOrDefaultAsync(p => p.Id == BuiltInProviders.RecurringIncome, ct);
            if (provider == null)
            {
                provider = CreateBuiltInProvider();
                await dbContext.Providers.AddAsync(provider, ct);
                await dbContext.SaveChangesAsync(ct);
            }

            var streams = CreateStreams(provider);
            await dbContext.Streams.AddRangeAsync(streams, ct);
            await dbContext.SaveChangesAsync(ct);

            logger.LogInformation("Seeded {StreamCount} demo streams with snapshots", streams.Count);
        }
    }

    /// <summary>
    /// Ensures all registered connectors have corresponding provider records in the database.
    /// </summary>
    private async Task EnsureConnectorProvidersExistAsync(IncomeDbContext dbContext, CancellationToken ct)
    {
        var allConnectors = connectorRegistry.GetAll();
        var existingProviderIds = await dbContext.Providers
            .Select(p => p.Id)
            .ToListAsync(ct);

        foreach (var connector in allConnectors)
        {
            if (existingProviderIds.Contains(connector.ProviderId))
                continue;

            var providerEntity = CreateProviderFromConnector(connector);
            await dbContext.Providers.AddAsync(providerEntity, ct);
            logger.LogInformation("Created provider for connector: {ProviderId} ({DisplayName})",
                connector.ProviderId, connector.DisplayName);
        }

        await dbContext.SaveChangesAsync(ct);
    }

    private static ProviderEntity CreateProviderFromConnector(IIncomeConnector connector)
    {
        var (providerType, syncFrequency) = connector.Kind switch
        {
            ConnectorKind.Syncable => (0, 2), // Exchange, Daily
            ConnectorKind.Recurring => (3, 3), // Manual, Manual
            _ => (3, 3)
        };

        return new ProviderEntity
        {
            Id = connector.ProviderId,
            Name = connector.DisplayName,
            Type = providerType,
            ConnectorKind = (int)connector.Kind,
            DefaultCurrency = connector.DefaultCurrency,
            SyncFrequency = syncFrequency,
            ConfigSchema = connector is ISyncableConnector syncable ? syncable.ConfigSchema : null,
            SupportedStreamTypes = (int)connector.SupportedStreamTypes
        };
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

    private static List<StreamEntity> CreateStreams(ProviderEntity provider)
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
            Category = "Other",
            OriginalCurrency = "USD",
            IsFixed = true,
            FixedPeriod = "Daily",
            EncryptedCredentials = null,
            SyncState = 0, // Active
            StreamType = 0, // Income
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
            Category = "Salary",
            OriginalCurrency = "USD",
            IsFixed = true,
            FixedPeriod = "Monthly",
            EncryptedCredentials = null,
            SyncState = 0, // Active
            StreamType = 0, // Income
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
            Category = "Salary",
            OriginalCurrency = "USD",
            IsFixed = true,
            FixedPeriod = "Monthly",
            EncryptedCredentials = null,
            SyncState = 0,
            StreamType = 0, // Income
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
            Category = "Rental",
            OriginalCurrency = "USD",
            IsFixed = true,
            FixedPeriod = "Monthly",
            EncryptedCredentials = null,
            SyncState = 0,
            StreamType = 0, // Income
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

        // Variable income - Freelance gigs (variable, not fixed)
        var gigsStream = new StreamEntity
        {
            Id = "stream-freelance-gigs",
            ProviderId = provider.Id,
            Name = "Freelance Gigs",
            Category = "Business",
            OriginalCurrency = "USD",
            IsFixed = false,
            FixedPeriod = null,
            EncryptedCredentials = null,
            SyncState = 0,
            StreamType = 0, // Income
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

        // ============================
        // OUTCOME STREAMS (Expenses)
        // ============================

        // Rent payment - Monthly $2,200 USD
        var rentExpenseStream = new StreamEntity
        {
            Id = "stream-expense-rent",
            ProviderId = provider.Id,
            Name = "Apartment Rent",
            Category = "Housing",
            OriginalCurrency = "USD",
            IsFixed = true,
            FixedPeriod = "Monthly",
            EncryptedCredentials = null,
            SyncState = 0,
            StreamType = 1, // Outcome
            LinkedIncomeStreamId = null, // Draws from global pool
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

        // Utilities - Monthly ~$150 USD (variable)
        var utilitiesStream = new StreamEntity
        {
            Id = "stream-expense-utilities",
            ProviderId = provider.Id,
            Name = "Utilities (Electric/Gas/Water)",
            Category = "Utilities",
            OriginalCurrency = "USD",
            IsFixed = true,
            FixedPeriod = "Monthly",
            EncryptedCredentials = null,
            SyncState = 0,
            StreamType = 1, // Outcome
            LinkedIncomeStreamId = null,
            LastSuccessAt = now,
            LastAttemptAt = now,
            LastError = null,
            NextScheduledAt = null,
            CreatedAt = now.AddMonths(-6),
            RecurringAmount = 150m,
            RecurringFrequency = (int)RecurringFrequency.Monthly,
            RecurringStartDate = DateOnly.FromDateTime(now.AddMonths(-6)).AddDays(15),
            Snapshots = GenerateMonthlySalarySnapshots(150m, 6, 0.15m) // 15% variance
        };
        streams.Add(utilitiesStream);

        // Netflix subscription - Monthly $15.99
        var netflixStream = new StreamEntity
        {
            Id = "stream-expense-netflix",
            ProviderId = provider.Id,
            Name = "Netflix",
            Category = "Subscription",
            OriginalCurrency = "USD",
            IsFixed = true,
            FixedPeriod = "Monthly",
            EncryptedCredentials = null,
            SyncState = 0,
            StreamType = 1, // Outcome
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

        // GitHub Pro - Monthly $4
        var githubStream = new StreamEntity
        {
            Id = "stream-expense-github",
            ProviderId = provider.Id,
            Name = "GitHub Pro",
            Category = "Software",
            OriginalCurrency = "USD",
            IsFixed = true,
            FixedPeriod = "Monthly",
            EncryptedCredentials = null,
            SyncState = 0,
            StreamType = 1, // Outcome
            LinkedIncomeStreamId = null,
            LastSuccessAt = now,
            LastAttemptAt = now,
            LastError = null,
            NextScheduledAt = null,
            CreatedAt = now.AddMonths(-4),
            RecurringAmount = 4m,
            RecurringFrequency = (int)RecurringFrequency.Monthly,
            RecurringStartDate = DateOnly.FromDateTime(now.AddMonths(-4)).AddDays(10),
            Snapshots = GenerateMonthlySalarySnapshots(4m, 4, 0.0m)
        };
        streams.Add(githubStream);

        // Cloud Services (Azure/AWS) - Monthly ~$85 (variable)
        var cloudStream = new StreamEntity
        {
            Id = "stream-expense-cloud",
            ProviderId = provider.Id,
            Name = "Azure Cloud Services",
            Category = "Software",
            OriginalCurrency = "USD",
            IsFixed = true,
            FixedPeriod = "Monthly",
            EncryptedCredentials = null,
            SyncState = 0,
            StreamType = 1, // Outcome
            LinkedIncomeStreamId = null,
            LastSuccessAt = now,
            LastAttemptAt = now,
            LastError = null,
            NextScheduledAt = null,
            CreatedAt = now.AddMonths(-5),
            RecurringAmount = 85m,
            RecurringFrequency = (int)RecurringFrequency.Monthly,
            RecurringStartDate = DateOnly.FromDateTime(now.AddMonths(-5)).AddDays(1),
            Snapshots = GenerateMonthlySalarySnapshots(85m, 5, 0.20m) // 20% variance
        };
        streams.Add(cloudStream);

        // Car Insurance - Monthly $180
        var carInsuranceStream = new StreamEntity
        {
            Id = "stream-expense-car-insurance",
            ProviderId = provider.Id,
            Name = "Car Insurance",
            Category = "Insurance",
            OriginalCurrency = "USD",
            IsFixed = true,
            FixedPeriod = "Monthly",
            EncryptedCredentials = null,
            SyncState = 0,
            StreamType = 1, // Outcome
            LinkedIncomeStreamId = null,
            LastSuccessAt = now,
            LastAttemptAt = now,
            LastError = null,
            NextScheduledAt = null,
            CreatedAt = now.AddMonths(-6),
            RecurringAmount = 180m,
            RecurringFrequency = (int)RecurringFrequency.Monthly,
            RecurringStartDate = DateOnly.FromDateTime(now.AddMonths(-6)).AddDays(20),
            Snapshots = GenerateMonthlySalarySnapshots(180m, 6, 0.0m)
        };
        streams.Add(carInsuranceStream);

        // Gym membership - Monthly $50
        var gymStream = new StreamEntity
        {
            Id = "stream-expense-gym",
            ProviderId = provider.Id,
            Name = "Gym Membership",
            Category = "Services",
            OriginalCurrency = "USD",
            IsFixed = true,
            FixedPeriod = "Monthly",
            EncryptedCredentials = null,
            SyncState = 0,
            StreamType = 1, // Outcome
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

            // Generate 1-4 payments per month with high variability
            var paymentsThisMonth = random.Next(1, 5);
            for (var p = 0; p < paymentsThisMonth; p++)
            {
                var day = random.Next(1, 28);
                var payDate = new DateOnly(monthDate.Year, monthDate.Month, day);

                if (payDate <= DateOnly.FromDateTime(now))
                {
                    // High variability: +/- volatility percent
                    var variation = 1 + (decimal)((random.NextDouble() * 2 - 1) * (double)volatility);
                    var amount = Math.Round(avgAmount / paymentsThisMonth * variation, 2);
                    amount = Math.Max(100m, amount); // Minimum $100 per gig

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
}
