namespace Income.Application.Connectors;

/// <summary>
/// Registry for discovering and resolving income connectors.
/// Implements Strategy pattern for selecting the appropriate connector.
/// </summary>
public interface IConnectorRegistry
{
    /// <summary>
    /// Gets all registered connectors.
    /// </summary>
    IReadOnlyList<IIncomeConnector> GetAll();

    /// <summary>
    /// Gets all syncable connectors (API-based).
    /// </summary>
    IReadOnlyList<ISyncableConnector> GetSyncable();

    /// <summary>
    /// Gets all recurring connectors (schedule-based).
    /// </summary>
    IReadOnlyList<IRecurringConnector> GetRecurring();

    /// <summary>
    /// Gets all CSV import connectors (bank imports).
    /// </summary>
    IReadOnlyList<ICsvImportConnector> GetCsvImport();

    /// <summary>
    /// Gets a connector by its provider ID.
    /// </summary>
    IIncomeConnector? GetById(string providerId);

    /// <summary>
    /// Gets a syncable connector by its provider ID.
    /// </summary>
    ISyncableConnector? GetSyncableById(string providerId);

    /// <summary>
    /// Gets a recurring connector by its provider ID.
    /// </summary>
    IRecurringConnector? GetRecurringById(string providerId);

    /// <summary>
    /// Gets a CSV import connector by its provider ID.
    /// </summary>
    ICsvImportConnector? GetCsvImportById(string providerId);
}
