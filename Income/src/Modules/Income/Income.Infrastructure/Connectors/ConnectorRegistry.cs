using Income.Application.Connectors;

namespace Income.Infrastructure.Connectors;

/// <summary>
/// Registry for discovering and resolving income connectors.
/// Uses Strategy pattern - connectors are injected via DI and resolved at runtime.
/// </summary>
internal sealed class ConnectorRegistry : IConnectorRegistry
{
    private readonly IReadOnlyList<IIncomeConnector> _allConnectors;
    private readonly IReadOnlyList<ISyncableConnector> _syncableConnectors;
    private readonly IReadOnlyList<IRecurringConnector> _recurringConnectors;

    public ConnectorRegistry(
        IEnumerable<ISyncableConnector> syncableConnectors,
        IEnumerable<IRecurringConnector> recurringConnectors)
    {
        _syncableConnectors = syncableConnectors.ToList();
        _recurringConnectors = recurringConnectors.ToList();
        _allConnectors = [.. _syncableConnectors, .. _recurringConnectors];
    }

    public IReadOnlyList<IIncomeConnector> GetAll() => _allConnectors;

    public IReadOnlyList<ISyncableConnector> GetSyncable() => _syncableConnectors;

    public IReadOnlyList<IRecurringConnector> GetRecurring() => _recurringConnectors;

    public IIncomeConnector? GetById(string providerId) =>
        _allConnectors.FirstOrDefault(c => c.ProviderId.Equals(providerId, StringComparison.OrdinalIgnoreCase));

    public ISyncableConnector? GetSyncableById(string providerId) =>
        _syncableConnectors.FirstOrDefault(c => c.ProviderId.Equals(providerId, StringComparison.OrdinalIgnoreCase));

    public IRecurringConnector? GetRecurringById(string providerId) =>
        _recurringConnectors.FirstOrDefault(c => c.ProviderId.Equals(providerId, StringComparison.OrdinalIgnoreCase));
}
