using FluentResults;
using Income.Contracts.Commands;
using Income.Contracts.DTOs;
using Income.Domain.StreamContext.ValueObjects;
using Income.Infrastructure.Features.Mapping;
using Income.Infrastructure.Persistence;
using Income.Infrastructure.Persistence.Mapping;
using Microsoft.EntityFrameworkCore;

namespace Income.Infrastructure.Features.Streams.Handlers;

internal sealed class UpdateStreamHandler(IncomeDbContext dbContext) : IUpdateStreamHandler
{
    public async Task<Result<StreamDto>> HandleAsync(UpdateStreamCommand command, CancellationToken ct = default)
    {
        var entity = await dbContext.Streams
            .Include(x => x.Snapshots)
            .FirstOrDefaultAsync(x => x.Id == command.StreamId, ct);

        if (entity is null)
            return Result.Fail<StreamDto>($"Stream with id '{command.StreamId}' not found");

        var stream = entity.ToDomain();

        if (!string.IsNullOrWhiteSpace(command.Name))
            stream.UpdateName(command.Name);

        if (!string.IsNullOrWhiteSpace(command.Category))
        {
            var category = StreamCategory.FromString(command.Category);
            if (category is null)
                return Result.Fail<StreamDto>($"Invalid category: {command.Category}");

            stream.UpdateCategory(category);
        }

        entity.UpdateFrom(stream);
        await dbContext.SaveChangesAsync(ct);

        return Result.Ok(stream.ToDto());
    }
}
