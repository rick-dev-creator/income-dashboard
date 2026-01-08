using Income.Contracts.DTOs;
using Income.Domain.ProviderContext.Aggregates;
using Income.Domain.StreamContext.Aggregates;
using Income.Domain.StreamContext.Entities;

namespace Income.Infrastructure.Features.Mapping;

internal static class DtoMappingExtensions
{
    internal static StreamDto ToDto(this IncomeStream stream)
    {
        return new StreamDto(
            Id: stream.Id.Value,
            ProviderId: stream.ProviderId.Value,
            Name: stream.Name,
            Category: stream.Category.Value,
            OriginalCurrency: stream.OriginalCurrency,
            IsFixed: stream.IsFixed,
            FixedPeriod: stream.FixedPeriod,
            HasCredentials: stream.HasCredentials,
            SyncStatus: stream.SyncStatus.ToDto(),
            CreatedAt: stream.CreatedAt,
            Snapshots: stream.Snapshots.Select(s => s.ToDto()).ToList(),
            RecurringAmount: stream.RecurringAmount,
            RecurringFrequency: stream.RecurringFrequency,
            RecurringStartDate: stream.RecurringStartDate);
    }

    internal static SyncStatusDto ToDto(this Domain.StreamContext.ValueObjects.SyncStatus status)
    {
        return new SyncStatusDto(
            State: status.State.ToString(),
            LastSuccessAt: status.LastSuccessAt,
            LastAttemptAt: status.LastAttemptAt,
            LastError: status.LastError,
            NextScheduledAt: status.NextScheduledAt);
    }

    internal static SnapshotDto ToDto(this DailySnapshot snapshot)
    {
        return new SnapshotDto(
            Id: snapshot.Id.Value,
            Date: snapshot.Date,
            OriginalAmount: snapshot.OriginalAmount,
            OriginalCurrency: snapshot.OriginalCurrency,
            UsdAmount: snapshot.UsdAmount,
            ExchangeRate: snapshot.ExchangeRate,
            RateSource: snapshot.RateSource,
            SnapshotAt: snapshot.SnapshotAt);
    }

    internal static ProviderDto ToDto(this Provider provider)
    {
        return new ProviderDto(
            Id: provider.Id.Value,
            Name: provider.Name,
            Type: provider.Type.ToString(),
            DefaultCurrency: provider.DefaultCurrency,
            SyncFrequency: provider.SyncFrequency.ToString(),
            ConfigSchema: provider.ConfigSchema);
    }
}
