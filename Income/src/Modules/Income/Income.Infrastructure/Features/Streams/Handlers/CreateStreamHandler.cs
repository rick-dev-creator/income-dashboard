using FluentResults;
using Income.Application.Services;
using Income.Contracts.Commands;
using Income.Contracts.DTOs;
using Income.Domain.ProviderContext.ValueObjects;
using Income.Domain.StreamContext.Aggregates;
using Income.Domain.StreamContext.Interfaces;
using Income.Domain.StreamContext.ValueObjects;
using Income.Infrastructure.Features.Mapping;
using Income.Infrastructure.Persistence;
using Income.Infrastructure.Persistence.Mapping;
using Microsoft.EntityFrameworkCore;

namespace Income.Infrastructure.Features.Streams.Handlers;

internal sealed class CreateStreamHandler(
    IDbContextFactory<IncomeDbContext> dbContextFactory,
    ICredentialEncryptor credentialEncryptor) : ICreateStreamHandler
{
    public async Task<Result<StreamDto>> HandleAsync(CreateStreamCommand command, CancellationToken ct = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var providerExists = await dbContext.Providers
            .AnyAsync(x => x.Id == command.ProviderId, ct);

        if (!providerExists)
            return Result.Fail<StreamDto>($"Provider with id '{command.ProviderId}' not found");

        var category = StreamCategory.FromString(command.Category);
        if (category is null)
            return Result.Fail<StreamDto>($"Invalid category: {command.Category}");

        var encryptedCredentials = !string.IsNullOrEmpty(command.Credentials)
            ? credentialEncryptor.Encrypt(command.Credentials)
            : null;

        var data = new CreateStreamData(
            ProviderId: new ProviderId(command.ProviderId),
            Name: command.Name,
            Category: category,
            OriginalCurrency: command.OriginalCurrency,
            IsFixed: command.IsFixed,
            FixedPeriod: command.FixedPeriod,
            EncryptedCredentials: encryptedCredentials,
            RecurringAmount: command.RecurringAmount,
            RecurringFrequency: command.RecurringFrequency,
            RecurringStartDate: command.RecurringStartDate);

        var result = IncomeStream.Create(data);
        if (result.IsFailed)
            return result.ToResult<StreamDto>();

        var entity = result.Value.ToEntity();
        await dbContext.Streams.AddAsync(entity, ct);
        await dbContext.SaveChangesAsync(ct);

        return Result.Ok(result.Value.ToDto());
    }

    private sealed record CreateStreamData(
        ProviderId ProviderId,
        string Name,
        StreamCategory Category,
        string OriginalCurrency,
        bool IsFixed,
        string? FixedPeriod,
        string? EncryptedCredentials,
        StreamType StreamType = StreamType.Income,
        StreamId? LinkedIncomeStreamId = null,
        decimal? RecurringAmount = null,
        int? RecurringFrequency = null,
        DateOnly? RecurringStartDate = null) : ICreateStreamData;
}
