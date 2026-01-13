using Income.Application.Connectors;
using Microsoft.Extensions.DependencyInjection;

namespace Income.Infrastructure.Connectors;

/// <summary>
/// Registry for discovering and resolving income connectors.
/// Uses Strategy pattern - connectors are resolved lazily from DI at runtime.
/// This ensures all connectors are available regardless of registration order.
/// </summary>
internal sealed class ConnectorRegistry : IConnectorRegistry
{
    private readonly IServiceProvider _serviceProvider;
    private IReadOnlyList<ISyncableConnector>? _syncableConnectors;
    private IReadOnlyList<IRecurringConnector>? _recurringConnectors;

    public ConnectorRegistry(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    private IReadOnlyList<ISyncableConnector> SyncableConnectors =>
        _syncableConnectors ??= _serviceProvider.GetServices<ISyncableConnector>().ToList();

    private IReadOnlyList<IRecurringConnector> RecurringConnectors =>
        _recurringConnectors ??= _serviceProvider.GetServices<IRecurringConnector>().ToList();

    public IReadOnlyList<IIncomeConnector> GetAll() =>
        [.. SyncableConnectors, .. RecurringConnectors];

    public IReadOnlyList<ISyncableConnector> GetSyncable() => SyncableConnectors;

    public IReadOnlyList<IRecurringConnector> GetRecurring() => RecurringConnectors;

    public IIncomeConnector? GetById(string providerId) =>
        GetAll().FirstOrDefault(c => c.ProviderId.Equals(providerId, StringComparison.OrdinalIgnoreCase));

    public ISyncableConnector? GetSyncableById(string providerId) =>
        SyncableConnectors.FirstOrDefault(c => c.ProviderId.Equals(providerId, StringComparison.OrdinalIgnoreCase));

    public IRecurringConnector? GetRecurringById(string providerId) =>
        RecurringConnectors.FirstOrDefault(c => c.ProviderId.Equals(providerId, StringComparison.OrdinalIgnoreCase));
}
