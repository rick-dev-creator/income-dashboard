using Income.Contracts.Commands;
using Income.Contracts.Queries;
using Income.Infrastructure.Features.Providers.Handlers;
using Income.Infrastructure.Tests.Fixtures;

namespace Income.Infrastructure.Tests.Features.Providers;

[Collection("Postgres")]
public class GetAllProvidersHandlerTests(PostgresFixture fixture)
{
    [Fact]
    public async Task HandleAsync_WithProviders_ReturnsAll()
    {
        // Arrange - Create some providers first
        await using var setupContext = fixture.CreateDbContext();
        var createHandler = new CreateProviderHandler(setupContext);

        var uniqueSuffix = Guid.NewGuid().ToString()[..8];
        await createHandler.HandleAsync(new CreateProviderCommand(
            Name: $"Provider1_{uniqueSuffix}",
            Type: "Exchange",
            DefaultCurrency: "USD",
            SyncFrequency: "Hourly",
            ConfigSchema: null));

        await createHandler.HandleAsync(new CreateProviderCommand(
            Name: $"Provider2_{uniqueSuffix}",
            Type: "Creator",
            DefaultCurrency: "USD",
            SyncFrequency: "Daily",
            ConfigSchema: null));

        // Act
        await using var context = fixture.CreateDbContext();
        var handler = new GetAllProvidersHandler(context);

        var result = await handler.HandleAsync(new GetAllProvidersQuery());

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBeGreaterThanOrEqualTo(2);
    }
}
