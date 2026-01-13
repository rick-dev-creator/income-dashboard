using Income.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Income.Infrastructure.Persistence.Configurations;

internal sealed class ProviderEntityConfiguration : IEntityTypeConfiguration<ProviderEntity>
{
    public void Configure(EntityTypeBuilder<ProviderEntity> builder)
    {
        builder.ToTable("providers");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasMaxLength(36)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Type)
            .HasColumnName("type")
            .IsRequired();

        builder.Property(x => x.ConnectorKind)
            .HasColumnName("connector_kind")
            .IsRequired();

        builder.Property(x => x.DefaultCurrency)
            .HasColumnName("default_currency")
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(x => x.SyncFrequency)
            .HasColumnName("sync_frequency")
            .IsRequired();

        builder.Property(x => x.ConfigSchema)
            .HasColumnName("config_schema")
            .HasColumnType("jsonb");

        builder.Property(x => x.SupportedStreamTypes)
            .HasColumnName("supported_stream_types")
            .HasDefaultValue(3) // Both by default
            .IsRequired();

        builder.HasIndex(x => x.Name)
            .IsUnique()
            .HasDatabaseName("ix_providers_name");
    }
}
