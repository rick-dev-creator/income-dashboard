namespace Income.Domain.StreamContext.ValueObjects;

internal enum SyncState
{
    Active,
    Syncing,
    Failed,
    Stale,
    Disabled
}

internal sealed record SyncStatus
{
    public SyncState State { get; private init; }
    public DateTime? LastSuccessAt { get; private init; }
    public DateTime? LastAttemptAt { get; private init; }
    public string? LastError { get; private init; }
    public DateTime? NextScheduledAt { get; private init; }

    private SyncStatus() { }

    public static SyncStatus Initial() => new()
    {
        State = SyncState.Active,
        LastSuccessAt = null,
        LastAttemptAt = null,
        LastError = null,
        NextScheduledAt = DateTime.UtcNow
    };

    public SyncStatus MarkSyncing() => this with
    {
        State = SyncState.Syncing,
        LastAttemptAt = DateTime.UtcNow
    };

    public SyncStatus MarkSuccess(DateTime? nextSync = null) => this with
    {
        State = SyncState.Active,
        LastSuccessAt = DateTime.UtcNow,
        LastError = null,
        NextScheduledAt = nextSync
    };

    public SyncStatus MarkFailed(string error) => this with
    {
        State = SyncState.Failed,
        LastError = error
    };

    public SyncStatus MarkStale() => this with
    {
        State = SyncState.Stale
    };

    public SyncStatus Disable() => this with
    {
        State = SyncState.Disabled,
        NextScheduledAt = null
    };

    public SyncStatus Enable() => this with
    {
        State = SyncState.Active,
        NextScheduledAt = DateTime.UtcNow
    };

    public bool CanSync => State is SyncState.Active or SyncState.Failed or SyncState.Stale;
    public bool IsHealthy => State == SyncState.Active && LastError is null;

    internal static SyncStatus Reconstruct(
        SyncState state,
        DateTime? lastSuccessAt,
        DateTime? lastAttemptAt,
        string? lastError,
        DateTime? nextScheduledAt) => new()
    {
        State = state,
        LastSuccessAt = lastSuccessAt,
        LastAttemptAt = lastAttemptAt,
        LastError = lastError,
        NextScheduledAt = nextScheduledAt
    };
}
