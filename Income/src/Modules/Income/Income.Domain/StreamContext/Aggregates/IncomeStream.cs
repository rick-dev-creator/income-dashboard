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
        DateTime createdAt,
        StreamType streamType,
        StreamId? linkedIncomeStreamId = null,
        decimal? recurringAmount = null,
        int? recurringFrequency = null,
        DateOnly? recurringStartDate = null)
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
        StreamType = streamType;
        LinkedIncomeStreamId = linkedIncomeStreamId;
        RecurringAmount = recurringAmount;
        RecurringFrequency = recurringFrequency;
        RecurringStartDate = recurringStartDate;
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

    // Stream type: Income (money in) or Outcome (money out)
    public StreamType StreamType { get; private init; }

    // For Outcome streams: optionally link to a specific Income stream
    public StreamId? LinkedIncomeStreamId { get; private set; }

    // Recurring stream properties (for schedule-based income)
    public decimal? RecurringAmount { get; private set; }
    public int? RecurringFrequency { get; private set; }
    public DateOnly? RecurringStartDate { get; private set; }

    /// <summary>
    /// Whether this stream is a recurring (schedule-based) stream vs a syncable (API-based) stream.
    /// </summary>
    public bool IsRecurring => RecurringAmount.HasValue && RecurringFrequency.HasValue && RecurringStartDate.HasValue;

    /// <summary>
    /// Whether this is an Income stream (money flowing in).
    /// </summary>
    public bool IsIncome => StreamType == StreamType.Income;

    /// <summary>
    /// Whether this is an Outcome stream (money flowing out).
    /// </summary>
    public bool IsOutcome => StreamType == StreamType.Outcome;

    /// <summary>
    /// The flow direction multiplier: +1 for income, -1 for outcome.
    /// </summary>
    public int FlowDirectionMultiplier => FlowDirection.FromStreamType(StreamType);

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

        // Validate linked income stream only for outcomes
        if (data.StreamType == StreamType.Income && data.LinkedIncomeStreamId is not null)
            errors.Add(new Error("Income streams cannot be linked to other streams"));

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
            createdAt: DateTime.UtcNow,
            streamType: data.StreamType,
            linkedIncomeStreamId: data.LinkedIncomeStreamId,
            recurringAmount: data.RecurringAmount,
            recurringFrequency: data.RecurringFrequency,
            recurringStartDate: data.RecurringStartDate);

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
            createdAt: data.CreatedAt,
            streamType: data.StreamType,
            linkedIncomeStreamId: data.LinkedIncomeStreamId,
            recurringAmount: data.RecurringAmount,
            recurringFrequency: data.RecurringFrequency,
            recurringStartDate: data.RecurringStartDate);

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

    internal void UpdateRecurringSettings(decimal amount, int frequency, DateOnly startDate)
    {
        RecurringAmount = amount;
        RecurringFrequency = frequency;
        RecurringStartDate = startDate;
    }

    internal void ClearRecurringSettings()
    {
        RecurringAmount = null;
        RecurringFrequency = null;
        RecurringStartDate = null;
    }

    /// <summary>
    /// Links this outcome stream to a specific income stream.
    /// Only valid for Outcome streams.
    /// </summary>
    internal Result LinkToIncomeStream(StreamId? incomeStreamId)
    {
        if (StreamType != StreamType.Outcome)
            return Result.Fail("Only outcome streams can be linked to income streams");

        LinkedIncomeStreamId = incomeStreamId;
        return Result.Ok();
    }

    /// <summary>
    /// Removes the link to any income stream, making this outcome draw from the global pool.
    /// </summary>
    internal void UnlinkFromIncomeStream()
    {
        LinkedIncomeStreamId = null;
    }

    public bool HasCredentials => !string.IsNullOrEmpty(EncryptedCredentials);

    /// <summary>
    /// Whether this outcome stream is linked to a specific income stream.
    /// </summary>
    public bool IsLinkedToIncomeStream => IsOutcome && LinkedIncomeStreamId is not null;

    public DailySnapshot? GetSnapshotByDate(DateOnly date) =>
        _snapshots.FirstOrDefault(s => s.Date == date);

    public DailySnapshot? GetLatestSnapshot() =>
        _snapshots.MaxBy(s => s.Date);

    public IEnumerable<DailySnapshot> GetSnapshotsInRange(DateOnly start, DateOnly end) =>
        _snapshots.Where(s => s.Date >= start && s.Date <= end).OrderBy(s => s.Date);

    public decimal GetTotalUsdInRange(DateOnly start, DateOnly end) =>
        GetSnapshotsInRange(start, end).Sum(s => s.UsdAmount);
}
