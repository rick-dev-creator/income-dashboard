using Income.Domain.StreamContext.Entities;
using Income.Domain.StreamContext.ValueObjects;
using Income.Infrastructure.Persistence.Entities;

namespace Income.Infrastructure.Persistence.Mapping;

internal static class SnapshotMappingExtensions
{
    internal static DailySnapshot ToDomain(this SnapshotEntity entity)
    {
        return DailySnapshot.Reconstruct(
            id: new SnapshotId(entity.Id),
            date: entity.Date,
            originalAmount: entity.OriginalAmount,
            originalCurrency: entity.OriginalCurrency,
            usdAmount: entity.UsdAmount,
            exchangeRate: entity.ExchangeRate,
            rateSource: entity.RateSource,
            snapshotAt: entity.SnapshotAt);
    }

    internal static SnapshotEntity ToEntity(this DailySnapshot domain, string streamId)
    {
        return new SnapshotEntity
        {
            Id = domain.Id.Value,
            StreamId = streamId,
            Date = domain.Date,
            OriginalAmount = domain.OriginalAmount,
            OriginalCurrency = domain.OriginalCurrency,
            UsdAmount = domain.UsdAmount,
            ExchangeRate = domain.ExchangeRate,
            RateSource = domain.RateSource,
            SnapshotAt = domain.SnapshotAt
        };
    }

    internal static void UpdateFrom(this SnapshotEntity entity, DailySnapshot domain)
    {
        entity.OriginalAmount = domain.OriginalAmount;
        entity.UsdAmount = domain.UsdAmount;
        entity.ExchangeRate = domain.ExchangeRate;
        entity.RateSource = domain.RateSource;
    }
}
