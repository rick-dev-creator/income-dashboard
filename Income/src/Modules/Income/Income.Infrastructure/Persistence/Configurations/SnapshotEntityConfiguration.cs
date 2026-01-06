using Income.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Income.Infrastructure.Persistence.Configurations;

internal sealed class SnapshotEntityConfiguration : IEntityTypeConfiguration<SnapshotEntity>
{
    public void Configure(EntityTypeBuilder<SnapshotEntity> builder)
    {
        builder.ToTable("snapshots");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasMaxLength(36)
            .IsRequired();

        builder.Property(x => x.StreamId)
            .HasColumnName("stream_id")
            .HasMaxLength(36)
            .IsRequired();

        builder.Property(x => x.Date)
            .HasColumnName("date")
            .IsRequired();

        builder.Property(x => x.OriginalAmount)
            .HasColumnName("original_amount")
            .HasPrecision(18, 8)
            .IsRequired();

        builder.Property(x => x.OriginalCurrency)
            .HasColumnName("original_currency")
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(x => x.UsdAmount)
            .HasColumnName("usd_amount")
            .HasPrecision(18, 8)
            .IsRequired();

        builder.Property(x => x.ExchangeRate)
            .HasColumnName("exchange_rate")
            .HasPrecision(18, 8)
            .IsRequired();

        builder.Property(x => x.RateSource)
            .HasColumnName("rate_source")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.SnapshotAt)
            .HasColumnName("snapshot_at")
            .IsRequired();

        builder.HasIndex(x => new { x.StreamId, x.Date })
            .IsUnique()
            .HasDatabaseName("ix_snapshots_stream_date");

        builder.HasIndex(x => x.Date)
            .HasDatabaseName("ix_snapshots_date");
    }
}
