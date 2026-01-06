using Income.Domain.StreamContext.Aggregates;
using Income.Domain.StreamContext.Entities;
using Income.Domain.StreamContext.ValueObjects;
using Income.Infrastructure.Persistence.Entities;

namespace Income.Infrastructure.Persistence.Mapping;

internal static class StreamMappingExtensions
{
    internal static IncomeStream ToDomain(this StreamEntity entity)
    {
        var snapshots = entity.Snapshots.Select(s => s.ToDomain());
        return IncomeStream.Reconstruct(entity, snapshots).Value;
    }

    internal static StreamEntity ToEntity(this IncomeStream domain)
    {
        return new StreamEntity
        {
            Id = domain.Id.Value,
            ProviderId = domain.ProviderId.Value,
            Name = domain.Name,
            Category = domain.Category.Value,
            OriginalCurrency = domain.OriginalCurrency,
            IsFixed = domain.IsFixed,
            FixedPeriod = domain.FixedPeriod,
            EncryptedCredentials = domain.EncryptedCredentials,
            SyncState = (int)domain.SyncStatus.State,
            LastSuccessAt = domain.SyncStatus.LastSuccessAt,
            LastAttemptAt = domain.SyncStatus.LastAttemptAt,
            LastError = domain.SyncStatus.LastError,
            NextScheduledAt = domain.SyncStatus.NextScheduledAt,
            CreatedAt = domain.CreatedAt,
            Snapshots = domain.Snapshots.Select(s => s.ToEntity(domain.Id.Value)).ToList()
        };
    }

    internal static void UpdateFrom(this StreamEntity entity, IncomeStream domain)
    {
        entity.Name = domain.Name;
        entity.Category = domain.Category.Value;
        entity.EncryptedCredentials = domain.EncryptedCredentials;
        entity.SyncState = (int)domain.SyncStatus.State;
        entity.LastSuccessAt = domain.SyncStatus.LastSuccessAt;
        entity.LastAttemptAt = domain.SyncStatus.LastAttemptAt;
        entity.LastError = domain.SyncStatus.LastError;
        entity.NextScheduledAt = domain.SyncStatus.NextScheduledAt;
    }
}
