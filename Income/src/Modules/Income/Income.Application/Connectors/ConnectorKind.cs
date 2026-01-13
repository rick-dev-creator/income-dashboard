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
