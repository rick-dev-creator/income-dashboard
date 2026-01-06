using FluentResults;
using Income.Application.Services;
using Income.Contracts.Commands;
using Income.Infrastructure.Persistence;
using Income.Infrastructure.Persistence.Mapping;
using Microsoft.EntityFrameworkCore;

namespace Income.Infrastructure.Features.Streams.Handlers;

internal sealed class UpdateCredentialsHandler(
    IncomeDbContext dbContext,
    ICredentialEncryptor credentialEncryptor) : IUpdateCredentialsHandler
{
    public async Task<Result> HandleAsync(UpdateCredentialsCommand command, CancellationToken ct = default)
    {
        var entity = await dbContext.Streams
            .FirstOrDefaultAsync(x => x.Id == command.StreamId, ct);

        if (entity is null)
            return Result.Fail($"Stream with id '{command.StreamId}' not found");

        var stream = entity.ToDomain();

        var encryptedCredentials = !string.IsNullOrEmpty(command.Credentials)
            ? credentialEncryptor.Encrypt(command.Credentials)
            : null;

        stream.UpdateCredentials(encryptedCredentials);
        entity.UpdateFrom(stream);
        await dbContext.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
