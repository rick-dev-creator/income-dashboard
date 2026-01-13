namespace Income.Domain.Tests.TestData;

internal record TestCreateStreamData(
    ProviderId ProviderId,
    string Name,
    StreamCategory Category,
    string OriginalCurrency,
    bool IsFixed,
    string? FixedPeriod,
    string? EncryptedCredentials = null,
    StreamType StreamType = StreamType.Income,
    StreamId? LinkedIncomeStreamId = null,
    decimal? RecurringAmount = null,
    int? RecurringFrequency = null,
    DateOnly? RecurringStartDate = null) : ICreateStreamData;

internal record TestRecordSnapshotData(
    DateOnly Date,
    Money OriginalMoney,
    ExchangeConversion Conversion) : IRecordSnapshotData;
