using FluentResults;
using Income.Contracts.DTOs;
using Income.Contracts.Queries;
using Income.Infrastructure.Features.Mapping;
using Income.Infrastructure.Persistence;
using Income.Infrastructure.Persistence.Mapping;
using Microsoft.EntityFrameworkCore;

namespace Income.Infrastructure.Features.Streams.Handlers;

internal sealed class GetAllStreamsHandler(IDbContextFactory<IncomeDbContext> dbContextFactory) : IGetAllStreamsHandler
{
    public async Task<Result<IReadOnlyList<StreamDto>>> HandleAsync(GetAllStreamsQuery query, CancellationToken ct = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var dbQuery = dbContext.Streams
            .Include(x => x.Snapshots)
            .AsNoTracking();

        // Filter by StreamType if specified
        if (query.StreamType.HasValue)
        {
            dbQuery = dbQuery.Where(x => x.StreamType == query.StreamType.Value);
        }

        var entities = await dbQuery.ToListAsync(ct);
        var dtos = entities.Select(e => e.ToDomain().ToDto()).ToList();
        return Result.Ok<IReadOnlyList<StreamDto>>(dtos);
    }
}
