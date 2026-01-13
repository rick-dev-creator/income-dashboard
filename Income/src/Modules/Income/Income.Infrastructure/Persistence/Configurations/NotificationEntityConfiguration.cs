using Income.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Income.Infrastructure.Persistence.Configurations;

internal sealed class NotificationEntityConfiguration : IEntityTypeConfiguration<NotificationEntity>
{
    public void Configure(EntityTypeBuilder<NotificationEntity> builder)
    {
        builder.ToTable("notifications");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasMaxLength(36)
            .IsRequired();

        builder.Property(x => x.Type)
            .HasColumnName("type")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Title)
            .HasColumnName("title")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Message)
            .HasColumnName("message")
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(x => x.StreamId)
            .HasColumnName("stream_id")
            .HasMaxLength(36);

        builder.Property(x => x.StreamName)
            .HasColumnName("stream_name")
            .HasMaxLength(200);

        builder.Property(x => x.IsRead)
            .HasColumnName("is_read")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.ReadAt)
            .HasColumnName("read_at");

        builder.HasIndex(x => x.IsRead)
            .HasDatabaseName("ix_notifications_is_read");

        builder.HasIndex(x => x.CreatedAt)
            .HasDatabaseName("ix_notifications_created_at");

        builder.HasIndex(x => x.Type)
            .HasDatabaseName("ix_notifications_type");
    }
}
