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
