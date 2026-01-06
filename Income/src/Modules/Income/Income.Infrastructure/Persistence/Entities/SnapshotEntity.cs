namespace Income.Infrastructure.Persistence.Entities;

internal sealed class SnapshotEntity
{
    public string Id { get; set; } = null!;
    public string StreamId { get; set; } = null!;
    public DateOnly Date { get; set; }
    public decimal OriginalAmount { get; set; }
    public string OriginalCurrency { get; set; } = null!;
    public decimal UsdAmount { get; set; }
    public decimal ExchangeRate { get; set; }
    public string RateSource { get; set; } = null!;
    public DateTime SnapshotAt { get; set; }

    public StreamEntity Stream { get; set; } = null!;
}
