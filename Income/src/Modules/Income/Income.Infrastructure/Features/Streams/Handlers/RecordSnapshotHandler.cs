using FluentResults;
using Income.Contracts.Commands;
using Income.Contracts.DTOs;
using Income.Domain.StreamContext.Interfaces;
using Income.Domain.StreamContext.ValueObjects;
using Income.Infrastructure.Features.Mapping;
using Income.Infrastructure.Persistence;
using Income.Infrastructure.Persistence.Mapping;
using Microsoft.EntityFrameworkCore;

namespace Income.Infrastructure.Features.Streams.Handlers;

internal sealed class RecordSnapshotHandler(IncomeDbContext dbContext) : IRecordSnapshotHandler
{
    public async Task<Result<SnapshotDto>> HandleAsync(RecordSnapshotCommand command, CancellationToken ct = default)
    {
        var entity = await dbContext.Streams
            .Include(x => x.Snapshots)
            .FirstOrDefaultAsync(x => x.Id == command.StreamId, ct);

        if (entity is null)
            return Result.Fail<SnapshotDto>($"Stream with id '{command.StreamId}' not found");

        var stream = entity.ToDomain();

        var moneyResult = Money.Create(command.OriginalAmount, command.OriginalCurrency);
        if (moneyResult.IsFailed)
            return moneyResult.ToResult<SnapshotDto>();

        var conversion = ExchangeConversion.Create(
            original: moneyResult.Value,
            rateToUsd: command.ExchangeRate,
            source: command.RateSource);

        var data = new RecordSnapshotData(
            Date: command.Date,
            OriginalMoney: moneyResult.Value,
            Conversion: conversion);

        var result = stream.RecordSnapshot(data);
        if (result.IsFailed)
            return result.ToResult<SnapshotDto>();

        // Check if it's an update or new snapshot
        var existingEntity = entity.Snapshots.FirstOrDefault(s => s.Date == command.Date);
        if (existingEntity is not null)
        {
            existingEntity.OriginalAmount = command.OriginalAmount;
            existingEntity.OriginalCurrency = command.OriginalCurrency;
            existingEntity.UsdAmount = command.UsdAmount;
            existingEntity.ExchangeRate = command.ExchangeRate;
            existingEntity.RateSource = command.RateSource;
        }
        else
        {
            entity.Snapshots.Add(result.Value.ToEntity(entity.Id));
        }

        await dbContext.SaveChangesAsync(ct);

        return Result.Ok(result.Value.ToDto());
    }

    private sealed record RecordSnapshotData(
        DateOnly Date,
        Money OriginalMoney,
        ExchangeConversion Conversion) : IRecordSnapshotData;
}
