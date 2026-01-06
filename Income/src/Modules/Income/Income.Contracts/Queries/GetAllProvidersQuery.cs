using FluentResults;
using Income.Contracts.DTOs;

namespace Income.Contracts.Queries;

public sealed record GetAllProvidersQuery;

public interface IGetAllProvidersHandler
{
    Task<Result<IReadOnlyList<ProviderDto>>> HandleAsync(GetAllProvidersQuery query, CancellationToken ct = default);
}
