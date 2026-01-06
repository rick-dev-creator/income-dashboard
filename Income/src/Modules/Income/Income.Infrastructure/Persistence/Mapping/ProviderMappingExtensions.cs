using Income.Domain.ProviderContext.Aggregates;
using Income.Domain.ProviderContext.ValueObjects;
using Income.Infrastructure.Persistence.Entities;

namespace Income.Infrastructure.Persistence.Mapping;

internal static class ProviderMappingExtensions
{
    internal static Provider ToDomain(this ProviderEntity entity)
    {
        return Provider.Reconstruct(
            id: new ProviderId(entity.Id),
            name: entity.Name,
            type: (ProviderType)entity.Type,
            defaultCurrency: entity.DefaultCurrency,
            syncFrequency: (SyncFrequency)entity.SyncFrequency,
            configSchema: entity.ConfigSchema);
    }

    internal static ProviderEntity ToEntity(this Provider domain)
    {
        return new ProviderEntity
        {
            Id = domain.Id.Value,
            Name = domain.Name,
            Type = (int)domain.Type,
            DefaultCurrency = domain.DefaultCurrency,
            SyncFrequency = (int)domain.SyncFrequency,
            ConfigSchema = domain.ConfigSchema
        };
    }
}
