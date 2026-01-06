namespace Income.Domain.Tests.TestData;

internal record TestCreateStreamData(
    ProviderId ProviderId,
    string Name,
    StreamCategory Category,
    string OriginalCurrency,
    bool IsFixed,
    string? FixedPeriod) : ICreateStreamData;

internal record TestRecordSnapshotData(
    DateOnly Date,
    Money OriginalMoney,
    ExchangeConversion Conversion) : IRecordSnapshotData;
