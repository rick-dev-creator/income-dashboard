using Domain.Shared.Kernel;
using FluentResults;
using Income.Domain.ProviderContext.ValueObjects;
using Income.Domain.StreamContext.Entities;
using Income.Domain.StreamContext.Events;
using Income.Domain.StreamContext.Interfaces;
using Income.Domain.StreamContext.ValueObjects;

namespace Income.Domain.StreamContext.Aggregates;

internal sealed class IncomeStream : AggregateRoot<StreamId>
{
    private readonly List<DailySnapshot> _snapshots = [];

    private IncomeStream(
        StreamId id,
        ProviderId providerId,
        string name,
        StreamCategory category,
        string originalCurrency,
        bool isFixed,
        string? fixedPeriod,
        string? encryptedCredentials,
        SyncStatus syncStatus,
        DateTime createdAt)
    {
        Id = id;
        ProviderId = providerId;
        _name = name;
        Category = category;
        OriginalCurrency = originalCurrency;
        IsFixed = isFixed;
        FixedPeriod = fixedPeriod;
        EncryptedCredentials = encryptedCredentials;
        SyncStatus = syncStatus;
        CreatedAt = createdAt;
    }

    public ProviderId ProviderId { get; private init; }

    private string _name = null!;
    public string Name
    {
        get => _name;
        private set => _name = !string.IsNullOrWhiteSpace(value)
            ? value.Trim()
            : throw new DomainException("Name is required");
    }

    public StreamCategory Category { get; private set; }
    public string OriginalCurrency { get; private init; } = null!;
    public bool IsFixed { get; private init; }
    public string? FixedPeriod { get; private init; }
    public string? EncryptedCredentials { get; private set; }
    public SyncStatus SyncStatus { get; private set; } = null!;
    public DateTime CreatedAt { get; private init; }
    public IReadOnlyList<DailySnapshot> Snapshots => _snapshots.AsReadOnly();

    internal static Result<IncomeStream> Create(ICreateStreamData data)
    {
        var errors = new List<IError>();

        if (string.IsNullOrWhiteSpace(data.Name))
            errors.Add(new Error("Name is required"));

        if (data.Category is null)
            errors.Add(new Error("Category is required"));

        if (string.IsNullOrWhiteSpace(data.OriginalCurrency))
            errors.Add(new Error("Currency is required"));

        if (data.IsFixed && string.IsNullOrWhiteSpace(data.FixedPeriod))
            errors.Add(new Error("Fixed period is required for fixed income streams"));

        if (errors.Count > 0)
            return Result.Fail<IncomeStream>(errors);

        var stream = new IncomeStream(
            id: StreamId.New(),
            providerId: data.ProviderId,
            name: data.Name,
            category: data.Category!,
            originalCurrency: data.OriginalCurrency.ToUpperInvariant(),
            isFixed: data.IsFixed,
            fixedPeriod: data.FixedPeriod,
            encryptedCredentials: data.EncryptedCredentials,
            syncStatus: SyncStatus.Initial(),
            createdAt: DateTime.UtcNow);

        stream.RaiseDomainEvent(new StreamCreatedDomainEvent(
            stream.Id,
            stream.ProviderId,
            stream.Name,
            stream.Category.Value));

        return Result.Ok(stream);
    }

    internal static Result<IncomeStream> Reconstruct(IStreamData data, IEnumerable<DailySnapshot> snapshots)
    {
        var stream = new IncomeStream(
            id: data.Id,
            providerId: data.ProviderId,
            name: data.Name,
            category: data.Category,
            originalCurrency: data.OriginalCurrency,
            isFixed: data.IsFixed,
            fixedPeriod: data.FixedPeriod,
            encryptedCredentials: data.EncryptedCredentials,
            syncStatus: data.SyncStatus,
            createdAt: data.CreatedAt);

        stream._snapshots.AddRange(snapshots);
        return Result.Ok(stream);
    }

    internal Result<DailySnapshot> RecordSnapshot(IRecordSnapshotData data)
    {
        if (SyncStatus.State == SyncState.Disabled)
            return Result.Fail<DailySnapshot>("Cannot record snapshot for disabled stream");

        var existingSnapshot = _snapshots.FirstOrDefault(s => s.Date == data.Date);

        if (existingSnapshot is not null)
        {
            existingSnapshot.UpdateAmount(data.OriginalMoney, data.Conversion);

            RaiseDomainEvent(new SnapshotUpdatedDomainEvent(
                Id,
                existingSnapshot.Id,
                data.Date,
                data.Conversion.UsdMoney.Amount));

            return Result.Ok(existingSnapshot);
        }

        var snapshot = DailySnapshot.Create(data.Date, data.OriginalMoney, data.Conversion);
        _snapshots.Add(snapshot);

        RaiseDomainEvent(new SnapshotRecordedDomainEvent(
            Id,
            snapshot.Id,
            data.Date,
            data.Conversion.UsdMoney.Amount));

        return Result.Ok(snapshot);
    }

    internal void UpdateName(string newName)
    {
        Name = newName;
    }

    internal void UpdateCategory(StreamCategory newCategory)
    {
        Category = newCategory ?? throw new DomainException("Category cannot be null");
    }

    internal void MarkSyncing()
    {
        SyncStatus = SyncStatus.MarkSyncing();
    }

    internal void MarkSyncSuccess(DateTime? nextSync = null)
    {
        SyncStatus = SyncStatus.MarkSuccess(nextSync);
    }

    internal void MarkSyncFailed(string error)
    {
        SyncStatus = SyncStatus.MarkFailed(error);

        RaiseDomainEvent(new StreamSyncFailedDomainEvent(Id, error));
    }

    internal void Disable()
    {
        SyncStatus = SyncStatus.Disable();
    }

    internal void Enable()
    {
        SyncStatus = SyncStatus.Enable();
    }

    internal void UpdateCredentials(string? encryptedCredentials)
    {
        EncryptedCredentials = encryptedCredentials;
    }

    public bool HasCredentials => !string.IsNullOrEmpty(EncryptedCredentials);

    public DailySnapshot? GetSnapshotByDate(DateOnly date) =>
        _snapshots.FirstOrDefault(s => s.Date == date);

    public DailySnapshot? GetLatestSnapshot() =>
        _snapshots.MaxBy(s => s.Date);

    public IEnumerable<DailySnapshot> GetSnapshotsInRange(DateOnly start, DateOnly end) =>
        _snapshots.Where(s => s.Date >= start && s.Date <= end).OrderBy(s => s.Date);

    public decimal GetTotalUsdInRange(DateOnly start, DateOnly end) =>
        GetSnapshotsInRange(start, end).Sum(s => s.UsdAmount);
}
