namespace Income.Application.Connectors;

/// <summary>
/// Base interface for all income connectors.
/// Each connector represents a way to fetch or generate income data.
/// </summary>
public interface IIncomeConnector
{
    /// <summary>
    /// Unique identifier for this connector (e.g., "binance", "patreon", "recurring-salary")
    /// </summary>
    string ProviderId { get; }

    /// <summary>
    /// Display name shown in UI (e.g., "Binance Exchange", "Monthly Salary")
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Category type (Exchange, Creator, Payment, Manual)
    /// </summary>
    string ProviderType { get; }

    /// <summary>
    /// Whether this connector syncs from API or generates recurring snapshots
    /// </summary>
    ConnectorKind Kind { get; }

    /// <summary>
    /// Default currency for this connector
    /// </summary>
    string DefaultCurrency { get; }

    /// <summary>
    /// Which stream types this connector supports (Income, Outcome, or Both).
    /// Exchange connectors typically only support Income (wallet balances).
    /// Manual/Recurring connectors typically support Both.
    /// </summary>
    SupportedStreamTypes SupportedStreamTypes { get; }
}
