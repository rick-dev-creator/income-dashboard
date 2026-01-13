namespace Income.Application.Connectors;

public enum ConnectorKind
{
    /// <summary>
    /// Pulls data from external APIs (Binance, Patreon, etc.)
    /// </summary>
    Syncable,

    /// <summary>
    /// Auto-generates snapshots based on recurring schedule (Salary, Rent, etc.)
    /// </summary>
    Recurring
}

public enum RecurringFrequency
{
    Daily,
    Weekly,
    BiWeekly,
    Monthly,
    Quarterly,
    Yearly
}

/// <summary>
/// Defines which stream types a provider/connector supports.
/// </summary>
[Flags]
public enum SupportedStreamTypes
{
    /// <summary>Only supports income streams (money coming in)</summary>
    Income = 1,

    /// <summary>Only supports outcome streams (money going out)</summary>
    Outcome = 2,

    /// <summary>Supports both income and outcome streams</summary>
    Both = Income | Outcome
}
