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
        var factory = fixture.CreateFactory();
        var createHandler = new CreateProviderHandler(factory);

        var uniqueSuffix = Guid.NewGuid().ToString()[..8];
        await createHandler.HandleAsync(new CreateProviderCommand(
            Name: $"Provider1_{uniqueSuffix}",
            Type: "Exchange",
            ConnectorKind: "Syncable",
            DefaultCurrency: "USD",
            SyncFrequency: "Hourly",
            ConfigSchema: null));

        await createHandler.HandleAsync(new CreateProviderCommand(
            Name: $"Provider2_{uniqueSuffix}",
            Type: "Creator",
            ConnectorKind: "Syncable",
            DefaultCurrency: "USD",
            SyncFrequency: "Daily",
            ConfigSchema: null));

        // Act
        var handler = new GetAllProvidersHandler(factory);

        var result = await handler.HandleAsync(new GetAllProvidersQuery());

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBeGreaterThanOrEqualTo(2);
    }
}
