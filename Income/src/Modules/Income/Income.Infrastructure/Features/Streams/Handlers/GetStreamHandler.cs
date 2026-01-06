using FluentResults;
using Income.Contracts.DTOs;
using Income.Contracts.Queries;
using Income.Infrastructure.Features.Mapping;
using Income.Infrastructure.Persistence;
using Income.Infrastructure.Persistence.Mapping;
using Microsoft.EntityFrameworkCore;

namespace Income.Infrastructure.Features.Streams.Handlers;

internal sealed class GetStreamHandler(IncomeDbContext dbContext) : IGetStreamHandler
{
    public async Task<Result<StreamDto>> HandleAsync(GetStreamQuery query, CancellationToken ct = default)
    {
        var entity = await dbContext.Streams
            .Include(x => x.Snapshots)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.StreamId, ct);

        if (entity is null)
            return Result.Fail<StreamDto>($"Stream with id '{query.StreamId}' not found");

        return Result.Ok(entity.ToDomain().ToDto());
    }
}
