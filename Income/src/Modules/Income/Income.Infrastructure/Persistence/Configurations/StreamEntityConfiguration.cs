using Income.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Income.Infrastructure.Persistence.Configurations;

internal sealed class StreamEntityConfiguration : IEntityTypeConfiguration<StreamEntity>
{
    public void Configure(EntityTypeBuilder<StreamEntity> builder)
    {
        builder.ToTable("streams");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasMaxLength(36)
            .IsRequired();

        builder.Property(x => x.ProviderId)
            .HasColumnName("provider_id")
            .HasMaxLength(36)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Category)
            .HasColumnName("category")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.OriginalCurrency)
            .HasColumnName("original_currency")
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(x => x.IsFixed)
            .HasColumnName("is_fixed")
            .IsRequired();

        builder.Property(x => x.FixedPeriod)
            .HasColumnName("fixed_period")
            .HasMaxLength(20);

        builder.Property(x => x.EncryptedCredentials)
            .HasColumnName("encrypted_credentials")
            .HasColumnType("text");

        builder.Property(x => x.SyncState)
            .HasColumnName("sync_state")
            .IsRequired();

        builder.Property(x => x.LastSuccessAt)
            .HasColumnName("last_success_at");

        builder.Property(x => x.LastAttemptAt)
            .HasColumnName("last_attempt_at");

        builder.Property(x => x.LastError)
            .HasColumnName("last_error")
            .HasMaxLength(500);

        builder.Property(x => x.NextScheduledAt)
            .HasColumnName("next_scheduled_at");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasMany(x => x.Snapshots)
            .WithOne(x => x.Stream)
            .HasForeignKey(x => x.StreamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.ProviderId)
            .HasDatabaseName("ix_streams_provider_id");

        builder.HasIndex(x => x.Category)
            .HasDatabaseName("ix_streams_category");
    }
}
