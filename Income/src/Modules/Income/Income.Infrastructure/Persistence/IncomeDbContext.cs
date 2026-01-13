using Income.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Income.Infrastructure.Persistence;

internal sealed class IncomeDbContext(DbContextOptions<IncomeDbContext> options) : DbContext(options)
{
    public DbSet<StreamEntity> Streams => Set<StreamEntity>();
    public DbSet<SnapshotEntity> Snapshots => Set<SnapshotEntity>();
    public DbSet<ProviderEntity> Providers => Set<ProviderEntity>();
    public DbSet<NotificationEntity> Notifications => Set<NotificationEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("income");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IncomeDbContext).Assembly);
    }
}
