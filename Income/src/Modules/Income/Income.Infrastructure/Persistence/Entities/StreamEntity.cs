using Income.Domain.ProviderContext.ValueObjects;
using Income.Domain.StreamContext.Interfaces;
using Income.Domain.StreamContext.ValueObjects;

namespace Income.Infrastructure.Persistence.Entities;

internal sealed class StreamEntity : IStreamData
{
    public string Id { get; set; } = null!;
    public string ProviderId { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Category { get; set; } = null!;
    public string OriginalCurrency { get; set; } = null!;
    public bool IsFixed { get; set; }
    public string? FixedPeriod { get; set; }
    public string? EncryptedCredentials { get; set; }
    public int SyncState { get; set; }
    public DateTime? LastSuccessAt { get; set; }
    public DateTime? LastAttemptAt { get; set; }
    public string? LastError { get; set; }
    public DateTime? NextScheduledAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public List<SnapshotEntity> Snapshots { get; set; } = [];

    StreamId IStreamData.Id => new(Id);
    ProviderId ICreateStreamData.ProviderId => new(ProviderId);
    string ICreateStreamData.Name => Name;
    StreamCategory ICreateStreamData.Category => StreamCategory.FromStringOrDefault(Category);
    string ICreateStreamData.OriginalCurrency => OriginalCurrency;
    bool ICreateStreamData.IsFixed => IsFixed;
    string? ICreateStreamData.FixedPeriod => FixedPeriod;
    string? ICreateStreamData.EncryptedCredentials => EncryptedCredentials;
    SyncStatus IStreamData.SyncStatus => ToSyncStatus();
    DateTime IStreamData.CreatedAt => CreatedAt;

    private SyncStatus ToSyncStatus()
    {
        return SyncStatus.Reconstruct(
            (Domain.StreamContext.ValueObjects.SyncState)SyncState,
            LastSuccessAt,
            LastAttemptAt,
            LastError,
            NextScheduledAt);
    }
}
