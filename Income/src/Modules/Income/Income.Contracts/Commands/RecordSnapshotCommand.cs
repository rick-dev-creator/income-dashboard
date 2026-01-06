using FluentResults;
using Income.Contracts.DTOs;

namespace Income.Contracts.Commands;

public sealed record RecordSnapshotCommand(
    string StreamId,
    DateOnly Date,
    decimal OriginalAmount,
    string OriginalCurrency,
    decimal UsdAmount,
    decimal ExchangeRate,
    string RateSource);

public interface IRecordSnapshotHandler
{
    Task<Result<SnapshotDto>> HandleAsync(RecordSnapshotCommand command, CancellationToken ct = default);
}
