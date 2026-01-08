using FluentResults;
using Income.Contracts.DTOs;

namespace Income.Contracts.Commands;

public sealed record CreateStreamCommand(
    string ProviderId,
    string Name,
    string Category,
    string OriginalCurrency,
    bool IsFixed,
    string? FixedPeriod,
    string? Credentials = null,
    decimal? RecurringAmount = null,
    int? RecurringFrequency = null,
    DateOnly? RecurringStartDate = null);

public interface ICreateStreamHandler
{
    Task<Result<StreamDto>> HandleAsync(CreateStreamCommand command, CancellationToken ct = default);
}
