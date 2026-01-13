using FluentResults;
using Income.Application.Services;
using Income.Application.Services.Streams;
using Income.Domain.ProviderContext.ValueObjects;
using Income.Domain.StreamContext.Aggregates;
using Income.Domain.StreamContext.Interfaces;
using Income.Domain.StreamContext.ValueObjects;
using Income.Infrastructure.Persistence;
using Income.Infrastructure.Persistence.Mapping;
using Microsoft.EntityFrameworkCore;

namespace Income.Infrastructure.Services;

internal sealed class StreamService(
    IDbContextFactory<IncomeDbContext> dbContextFactory,
    ICredentialEncryptor credentialEncryptor) : IStreamService
{
    public async Task<Result<IReadOnlyList<StreamListItem>>> GetAllAsync(CancellationToken ct = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var entities = await dbContext.Streams
            .Include(x => x.Snapshots)
            .AsNoTracking()
            .ToListAsync(ct);

        var items = entities.Select(MapToListItem).ToList();
        return Result.Ok<IReadOnlyList<StreamListItem>>(items);
    }

    public async Task<Result<StreamDetail>> GetByIdAsync(string streamId, CancellationToken ct = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var entity = await dbContext.Streams
            .Include(x => x.Snapshots)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == streamId, ct);

        if (entity is null)
            return Result.Fail<StreamDetail>($"Stream with id '{streamId}' not found");

        return Result.Ok(MapToDetail(entity));
    }

    public async Task<Result<IReadOnlyList<StreamListItem>>> GetByProviderAsync(string providerId, CancellationToken ct = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var entities = await dbContext.Streams
            .Include(x => x.Snapshots)
            .AsNoTracking()
            .Where(x => x.ProviderId == providerId)
            .ToListAsync(ct);

        var items = entities.Select(MapToListItem).ToList();
        return Result.Ok<IReadOnlyList<StreamListItem>>(items);
    }

    public async Task<Result<StreamDetail>> CreateAsync(CreateStreamRequest request, CancellationToken ct = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var providerExists = await dbContext.Providers
            .AnyAsync(x => x.Id == request.ProviderId, ct);

        if (!providerExists)
            return Result.Fail<StreamDetail>($"Provider with id '{request.ProviderId}' not found");

        var category = StreamCategory.FromString(request.Category);
        if (category is null)
            return Result.Fail<StreamDetail>($"Invalid category: {request.Category}");

        var encryptedCredentials = !string.IsNullOrEmpty(request.Credentials)
            ? credentialEncryptor.Encrypt(request.Credentials)
            : null;

        var data = new CreateStreamData(
            ProviderId: new ProviderId(request.ProviderId),
            Name: request.Name,
            Category: category,
            OriginalCurrency: request.OriginalCurrency,
            IsFixed: request.IsFixed,
            FixedPeriod: request.FixedPeriod,
            EncryptedCredentials: encryptedCredentials,
            RecurringAmount: request.RecurringAmount,
            RecurringFrequency: request.RecurringFrequency,
            RecurringStartDate: request.RecurringStartDate);

        var result = IncomeStream.Create(data);
        if (result.IsFailed)
            return result.ToResult<StreamDetail>();

        var entity = result.Value.ToEntity();
        await dbContext.Streams.AddAsync(entity, ct);
        await dbContext.SaveChangesAsync(ct);

        return Result.Ok(MapToDetail(entity));
    }

    public async Task<Result<StreamDetail>> UpdateAsync(UpdateStreamRequest request, CancellationToken ct = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var entity = await dbContext.Streams
            .Include(x => x.Snapshots)
            .FirstOrDefaultAsync(x => x.Id == request.StreamId, ct);

        if (entity is null)
            return Result.Fail<StreamDetail>($"Stream with id '{request.StreamId}' not found");

        var stream = entity.ToDomain();

        if (!string.IsNullOrWhiteSpace(request.Name))
            stream.UpdateName(request.Name);

        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            var category = StreamCategory.FromString(request.Category);
            if (category is null)
                return Result.Fail<StreamDetail>($"Invalid category: {request.Category}");

            stream.UpdateCategory(category);
        }

        entity.UpdateFrom(stream);
        await dbContext.SaveChangesAsync(ct);

        return Result.Ok(MapToDetail(entity));
    }

    public async Task<Result> DeleteAsync(string streamId, CancellationToken ct = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var entity = await dbContext.Streams
            .FirstOrDefaultAsync(x => x.Id == streamId, ct);

        if (entity is null)
            return Result.Fail($"Stream with id '{streamId}' not found");

        dbContext.Streams.Remove(entity);
        await dbContext.SaveChangesAsync(ct);

        return Result.Ok();
    }

    public async Task<Result<SnapshotItem>> RecordSnapshotAsync(RecordSnapshotRequest request, CancellationToken ct = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var entity = await dbContext.Streams
            .Include(x => x.Snapshots)
            .FirstOrDefaultAsync(x => x.Id == request.StreamId, ct);

        if (entity is null)
            return Result.Fail<SnapshotItem>($"Stream with id '{request.StreamId}' not found");

        var stream = entity.ToDomain();

        var moneyResult = Money.Create(request.OriginalAmount, request.OriginalCurrency);
        if (moneyResult.IsFailed)
            return moneyResult.ToResult<SnapshotItem>();

        var conversion = ExchangeConversion.Create(
            original: moneyResult.Value,
            rateToUsd: request.ExchangeRate,
            source: request.RateSource);

        var data = new RecordSnapshotData(
            Date: request.Date,
            OriginalMoney: moneyResult.Value,
            Conversion: conversion);

        var result = stream.RecordSnapshot(data);
        if (result.IsFailed)
            return result.ToResult<SnapshotItem>();

        // Check if it's an update or new snapshot
        var existingEntity = entity.Snapshots.FirstOrDefault(s => s.Date == request.Date);
        if (existingEntity is not null)
        {
            existingEntity.OriginalAmount = request.OriginalAmount;
            existingEntity.OriginalCurrency = request.OriginalCurrency;
            existingEntity.UsdAmount = request.UsdAmount;
            existingEntity.ExchangeRate = request.ExchangeRate;
            existingEntity.RateSource = request.RateSource;
        }
        else
        {
            entity.Snapshots.Add(result.Value.ToEntity(entity.Id));
        }

        await dbContext.SaveChangesAsync(ct);

        return Result.Ok(MapSnapshotToItem(result.Value));
    }

    public async Task<Result> UpdateCredentialsAsync(string streamId, string? credentials, CancellationToken ct = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var entity = await dbContext.Streams
            .FirstOrDefaultAsync(x => x.Id == streamId, ct);

        if (entity is null)
            return Result.Fail($"Stream with id '{streamId}' not found");

        var stream = entity.ToDomain();

        var encryptedCredentials = !string.IsNullOrEmpty(credentials)
            ? credentialEncryptor.Encrypt(credentials)
            : null;

        stream.UpdateCredentials(encryptedCredentials);
        entity.UpdateFrom(stream);
        await dbContext.SaveChangesAsync(ct);

        return Result.Ok();
    }

    public async Task<Result> ToggleStatusAsync(string streamId, CancellationToken ct = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var entity = await dbContext.Streams
            .FirstOrDefaultAsync(x => x.Id == streamId, ct);

        if (entity is null)
            return Result.Fail($"Stream with id '{streamId}' not found");

        var stream = entity.ToDomain();

        // Toggle: if disabled -> enable, otherwise -> disable
        if (entity.SyncState == (int)Domain.StreamContext.ValueObjects.SyncState.Disabled)
            stream.Enable();
        else
            stream.Disable();

        entity.UpdateFrom(stream);
        await dbContext.SaveChangesAsync(ct);

        return Result.Ok();
    }

    private static StreamListItem MapToListItem(Persistence.Entities.StreamEntity entity)
    {
        var lastSnapshot = entity.Snapshots
            .OrderByDescending(s => s.Date)
            .FirstOrDefault();

        return new StreamListItem(
            Id: entity.Id,
            Name: entity.Name,
            ProviderId: entity.ProviderId,
            Category: entity.Category,
            OriginalCurrency: entity.OriginalCurrency,
            IsFixed: entity.IsFixed,
            FixedPeriod: entity.FixedPeriod,
            HasCredentials: !string.IsNullOrEmpty(entity.EncryptedCredentials),
            SnapshotCount: entity.Snapshots.Count,
            LastSnapshot: lastSnapshot is not null ? MapSnapshotEntityToItem(lastSnapshot) : null,
            SyncState: (StreamSyncState)entity.SyncState);
    }

    private static StreamDetail MapToDetail(Persistence.Entities.StreamEntity entity)
    {
        return new StreamDetail(
            Id: entity.Id,
            Name: entity.Name,
            ProviderId: entity.ProviderId,
            Category: entity.Category,
            OriginalCurrency: entity.OriginalCurrency,
            IsFixed: entity.IsFixed,
            FixedPeriod: entity.FixedPeriod,
            HasCredentials: !string.IsNullOrEmpty(entity.EncryptedCredentials),
            Snapshots: entity.Snapshots
                .OrderByDescending(s => s.Date)
                .Select(MapSnapshotEntityToItem)
                .ToList(),
            SyncState: (StreamSyncState)entity.SyncState);
    }

    private static SnapshotItem MapSnapshotEntityToItem(Persistence.Entities.SnapshotEntity entity)
    {
        return new SnapshotItem(
            Id: entity.Id,
            Date: entity.Date,
            OriginalAmount: entity.OriginalAmount,
            OriginalCurrency: entity.OriginalCurrency,
            UsdAmount: entity.UsdAmount,
            ExchangeRate: entity.ExchangeRate,
            RateSource: entity.RateSource);
    }

    private static SnapshotItem MapSnapshotToItem(Domain.StreamContext.Entities.DailySnapshot snapshot)
    {
        return new SnapshotItem(
            Id: snapshot.Id.Value,
            Date: snapshot.Date,
            OriginalAmount: snapshot.GetOriginalMoney().Amount,
            OriginalCurrency: snapshot.GetOriginalMoney().Currency,
            UsdAmount: snapshot.GetUsdMoney().Amount,
            ExchangeRate: snapshot.ExchangeRate,
            RateSource: snapshot.RateSource);
    }

    private sealed record CreateStreamData(
        ProviderId ProviderId,
        string Name,
        StreamCategory Category,
        string OriginalCurrency,
        bool IsFixed,
        string? FixedPeriod,
        string? EncryptedCredentials,
        decimal? RecurringAmount = null,
        int? RecurringFrequency = null,
        DateOnly? RecurringStartDate = null) : ICreateStreamData;

    private sealed record RecordSnapshotData(
        DateOnly Date,
        Money OriginalMoney,
        ExchangeConversion Conversion) : IRecordSnapshotData;
}
