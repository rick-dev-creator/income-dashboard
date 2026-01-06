using FluentResults;
using Income.Contracts.Commands;
using Income.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Income.Infrastructure.Features.Streams.Handlers;

internal sealed class DeleteStreamHandler(IncomeDbContext dbContext) : IDeleteStreamHandler
{
    public async Task<Result> HandleAsync(DeleteStreamCommand command, CancellationToken ct = default)
    {
        var entity = await dbContext.Streams
            .FirstOrDefaultAsync(x => x.Id == command.StreamId, ct);

        if (entity is null)
            return Result.Fail($"Stream with id '{command.StreamId}' not found");

        dbContext.Streams.Remove(entity);
        await dbContext.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
