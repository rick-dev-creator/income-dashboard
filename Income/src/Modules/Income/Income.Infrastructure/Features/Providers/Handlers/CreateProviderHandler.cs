using FluentResults;
using Income.Contracts.Commands;
using Income.Contracts.DTOs;
using Income.Domain.ProviderContext.Aggregates;
using Income.Infrastructure.Features.Mapping;
using Income.Infrastructure.Persistence;
using Income.Infrastructure.Persistence.Mapping;
using Microsoft.EntityFrameworkCore;

namespace Income.Infrastructure.Features.Providers.Handlers;

internal sealed class CreateProviderHandler(IDbContextFactory<IncomeDbContext> dbContextFactory) : ICreateProviderHandler
{
    public async Task<Result<ProviderDto>> HandleAsync(CreateProviderCommand command, CancellationToken ct = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var exists = await dbContext.Providers
            .AnyAsync(x => x.Name == command.Name, ct);

        if (exists)
            return Result.Fail<ProviderDto>($"Provider with name '{command.Name}' already exists");

        if (!Enum.TryParse<ProviderType>(command.Type, true, out var providerType))
            return Result.Fail<ProviderDto>($"Invalid provider type: {command.Type}");

        if (!Enum.TryParse<ConnectorKind>(command.ConnectorKind, true, out var connectorKind))
            return Result.Fail<ProviderDto>($"Invalid connector kind: {command.ConnectorKind}");

        if (!Enum.TryParse<SyncFrequency>(command.SyncFrequency, true, out var syncFrequency))
            return Result.Fail<ProviderDto>($"Invalid sync frequency: {command.SyncFrequency}");

        var result = Provider.Create(
            name: command.Name,
            type: providerType,
            connectorKind: connectorKind,
            defaultCurrency: command.DefaultCurrency,
            syncFrequency: syncFrequency,
            configSchema: command.ConfigSchema);

        if (result.IsFailed)
            return result.ToResult<ProviderDto>();

        var entity = result.Value.ToEntity();
        await dbContext.Providers.AddAsync(entity, ct);
        await dbContext.SaveChangesAsync(ct);

        return Result.Ok(result.Value.ToDto());
    }
}
