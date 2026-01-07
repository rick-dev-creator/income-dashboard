using Income.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Income.Infrastructure.Tests.Fixtures;

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("flowmetrics_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await InitializeDatabaseAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    private async Task InitializeDatabaseAsync()
    {
        await using var context = CreateDbContextInternal();
        await context.Database.EnsureCreatedAsync();
    }

    internal IncomeDbContext CreateDbContextInternal()
    {
        var options = new DbContextOptionsBuilder<IncomeDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new IncomeDbContext(options);
    }

    internal IDbContextFactory<IncomeDbContext> CreateDbContextFactory()
    {
        return new TestDbContextFactory(ConnectionString);
    }
}

internal sealed class TestDbContextFactory(string connectionString) : IDbContextFactory<IncomeDbContext>
{
    public IncomeDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<IncomeDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new IncomeDbContext(options);
    }
}

[CollectionDefinition("Postgres")]
public class PostgresCollection : ICollectionFixture<PostgresFixture>
{
}

internal static class PostgresFixtureExtensions
{
    internal static IncomeDbContext CreateDbContext(this PostgresFixture fixture) =>
        fixture.CreateDbContextInternal();

    internal static IDbContextFactory<IncomeDbContext> CreateFactory(this PostgresFixture fixture) =>
        fixture.CreateDbContextFactory();
}
