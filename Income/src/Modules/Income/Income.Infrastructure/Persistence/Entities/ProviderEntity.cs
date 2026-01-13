namespace Income.Infrastructure.Persistence.Entities;

internal sealed class ProviderEntity
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public int Type { get; set; }
    public int ConnectorKind { get; set; }
    public string DefaultCurrency { get; set; } = null!;
    public int SyncFrequency { get; set; }
    public string? ConfigSchema { get; set; }

    /// <summary>
    /// Bitmask indicating supported stream types (1=Income, 2=Outcome, 3=Both).
    /// </summary>
    public int SupportedStreamTypes { get; set; } = 3; // Default: Both
}
