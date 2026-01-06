using FluentResults;
using Income.Contracts.DTOs;

namespace Income.Contracts.Queries;

public sealed record GetStreamsByProviderQuery(string ProviderId);

public interface IGetStreamsByProviderHandler
{
    Task<Result<IReadOnlyList<StreamDto>>> HandleAsync(GetStreamsByProviderQuery query, CancellationToken ct = default);
}
