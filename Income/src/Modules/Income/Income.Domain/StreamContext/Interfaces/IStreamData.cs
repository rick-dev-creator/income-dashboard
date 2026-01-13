using Income.Domain.ProviderContext.ValueObjects;
using Income.Domain.StreamContext.ValueObjects;

namespace Income.Domain.StreamContext.Interfaces;

internal interface ICreateStreamData
{
    ProviderId ProviderId { get; }
    string Name { get; }
    StreamCategory Category { get; }
    string OriginalCurrency { get; }
    bool IsFixed { get; }
    string? FixedPeriod { get; }
    string? EncryptedCredentials { get; }

    // Stream type (Income or Outcome)
    StreamType StreamType { get; }

    // For Outcome streams: optionally link to a specific Income stream
    // If null, the outcome draws from the global pool (sum of all incomes)
    StreamId? LinkedIncomeStreamId { get; }

    // Recurring stream fields (for schedule-based income like salary)
    decimal? RecurringAmount { get; }
    int? RecurringFrequency { get; } // Maps to RecurringFrequency enum
    DateOnly? RecurringStartDate { get; }
}

internal interface IStreamData : ICreateStreamData
{
    StreamId Id { get; }
    SyncStatus SyncStatus { get; }
    DateTime CreatedAt { get; }
}

internal interface IRecordSnapshotData
{
    DateOnly Date { get; }
    Money OriginalMoney { get; }
    ExchangeConversion Conversion { get; }
}
