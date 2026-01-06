namespace Income.Infrastructure.Seeding;

internal interface ISeedDataGenerator
{
    Task SeedAsync(CancellationToken ct = default);
    Task<bool> HasDataAsync(CancellationToken ct = default);
}
