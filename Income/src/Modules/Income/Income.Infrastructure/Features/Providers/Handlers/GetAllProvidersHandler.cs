using FluentResults;
using Income.Contracts.DTOs;
using Income.Contracts.Queries;
using Income.Infrastructure.Features.Mapping;
using Income.Infrastructure.Persistence;
using Income.Infrastructure.Persistence.Mapping;
using Microsoft.EntityFrameworkCore;

namespace Income.Infrastructure.Features.Providers.Handlers;

internal sealed class GetAllProvidersHandler(IncomeDbContext dbContext) : IGetAllProvidersHandler
{
    public async Task<Result<IReadOnlyList<ProviderDto>>> HandleAsync(GetAllProvidersQuery query, CancellationToken ct = default)
    {
        var entities = await dbContext.Providers
            .AsNoTracking()
            .ToListAsync(ct);

        var dtos = entities.Select(e => e.ToDomain().ToDto()).ToList();
        return Result.Ok<IReadOnlyList<ProviderDto>>(dtos);
    }
}
