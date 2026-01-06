using FluentResults;
using Income.Contracts.DTOs;

namespace Income.Contracts.Queries;

public sealed record GetProviderQuery(string ProviderId);

public interface IGetProviderHandler
{
    Task<Result<ProviderDto>> HandleAsync(GetProviderQuery query, CancellationToken ct = default);
}
