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

        // Only ensure connector providers exist - no demo streams by default
        await EnsureConnectorProvidersExistAsync(dbContext, ct);

        // Ensure the recurring income provider exists (for manual entries)
        var provider = await dbContext.Providers.FirstOrDefaultAsync(p => p.Id == BuiltInProviders.RecurringIncome, ct);
        if (provider == null)
        {
            provider = CreateBuiltInProvider();
            await dbContext.Providers.AddAsync(provider, ct);
            await dbContext.SaveChangesAsync(ct);
            logger.LogInformation("Created built-in provider: {ProviderId}", BuiltInProviders.RecurringIncome);
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
            ConnectorKind.CsvImport => (3, 3), // Manual, Manual (bank CSV import)
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
}
