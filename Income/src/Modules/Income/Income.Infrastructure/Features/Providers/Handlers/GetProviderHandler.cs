using FluentResults;
using Income.Contracts.DTOs;
using Income.Contracts.Queries;
using Income.Infrastructure.Features.Mapping;
using Income.Infrastructure.Persistence;
using Income.Infrastructure.Persistence.Mapping;
using Microsoft.EntityFrameworkCore;

namespace Income.Infrastructure.Features.Providers.Handlers;

internal sealed class GetProviderHandler(IDbContextFactory<IncomeDbContext> dbContextFactory) : IGetProviderHandler
{
    public async Task<Result<ProviderDto>> HandleAsync(GetProviderQuery query, CancellationToken ct = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var entity = await dbContext.Providers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.ProviderId, ct);

        if (entity is null)
            return Result.Fail<ProviderDto>($"Provider with id '{query.ProviderId}' not found");

        return Result.Ok(entity.ToDomain().ToDto());
    }
}
