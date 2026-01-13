using FluentResults;
using Income.Application.Connectors;
using Income.Application.Services.Providers;
using Income.Domain.ProviderContext.Aggregates;
using Income.Infrastructure.Persistence;
using Income.Infrastructure.Persistence.Mapping;
using Microsoft.EntityFrameworkCore;

namespace Income.Infrastructure.Services;

internal sealed class ProviderService(IDbContextFactory<IncomeDbContext> dbContextFactory) : IProviderService
{
    public async Task<Result<IReadOnlyList<ProviderListItem>>> GetAllAsync(CancellationToken ct = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var entities = await dbContext.Providers
            .AsNoTracking()
            .ToListAsync(ct);

        var items = entities.Select(MapToListItem).ToList();
        return Result.Ok<IReadOnlyList<ProviderListItem>>(items);
    }

    public async Task<Result<ProviderDetail>> GetByIdAsync(string providerId, CancellationToken ct = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var entity = await dbContext.Providers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == providerId, ct);

        if (entity is null)
            return Result.Fail<ProviderDetail>($"Provider with id '{providerId}' not found");

        return Result.Ok(MapToDetail(entity));
    }

    public async Task<Result<ProviderDetail>> CreateAsync(CreateProviderRequest request, CancellationToken ct = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var exists = await dbContext.Providers
            .AnyAsync(x => x.Name == request.Name, ct);

        if (exists)
            return Result.Fail<ProviderDetail>($"Provider with name '{request.Name}' already exists");

        if (!Enum.TryParse<ProviderType>(request.Type, true, out var providerType))
            return Result.Fail<ProviderDetail>($"Invalid provider type: {request.Type}");

        if (!Enum.TryParse<Domain.ProviderContext.Aggregates.ConnectorKind>(request.ConnectorKind, true, out var connectorKind))
            return Result.Fail<ProviderDetail>($"Invalid connector kind: {request.ConnectorKind}");

        if (!Enum.TryParse<SyncFrequency>(request.SyncFrequency, true, out var syncFrequency))
            return Result.Fail<ProviderDetail>($"Invalid sync frequency: {request.SyncFrequency}");

        var result = Provider.Create(
            name: request.Name,
            type: providerType,
            connectorKind: connectorKind,
            defaultCurrency: request.DefaultCurrency,
            syncFrequency: syncFrequency,
            configSchema: request.ConfigSchema);

        if (result.IsFailed)
            return result.ToResult<ProviderDetail>();

        var entity = result.Value.ToEntity();
        await dbContext.Providers.AddAsync(entity, ct);
        await dbContext.SaveChangesAsync(ct);

        return Result.Ok(MapToDetail(entity));
    }

    private static ProviderListItem MapToListItem(Persistence.Entities.ProviderEntity entity)
    {
        return new ProviderListItem(
            Id: entity.Id,
            Name: entity.Name,
            Type: ((ProviderType)entity.Type).ToString(),
            ConnectorKind: ((Domain.ProviderContext.Aggregates.ConnectorKind)entity.ConnectorKind).ToString(),
            DefaultCurrency: entity.DefaultCurrency,
            SyncFrequency: ((SyncFrequency)entity.SyncFrequency).ToString(),
            ConfigSchema: entity.ConfigSchema,
            SupportedStreamTypes: (SupportedStreamTypes)entity.SupportedStreamTypes);
    }

    private static ProviderDetail MapToDetail(Persistence.Entities.ProviderEntity entity)
    {
        return new ProviderDetail(
            Id: entity.Id,
            Name: entity.Name,
            Type: ((ProviderType)entity.Type).ToString(),
            ConnectorKind: ((Domain.ProviderContext.Aggregates.ConnectorKind)entity.ConnectorKind).ToString(),
            DefaultCurrency: entity.DefaultCurrency,
            SyncFrequency: ((SyncFrequency)entity.SyncFrequency).ToString(),
            ConfigSchema: entity.ConfigSchema,
            SupportedStreamTypes: (SupportedStreamTypes)entity.SupportedStreamTypes);
    }
}
