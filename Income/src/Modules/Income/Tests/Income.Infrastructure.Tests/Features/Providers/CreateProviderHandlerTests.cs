using Income.Contracts.Commands;
using Income.Infrastructure.Features.Providers.Handlers;
using Income.Infrastructure.Tests.Fixtures;

namespace Income.Infrastructure.Tests.Features.Providers;

[Collection("Postgres")]
public class CreateProviderHandlerTests(PostgresFixture fixture)
{
    [Fact]
    public async Task HandleAsync_ValidCommand_CreatesProvider()
    {
        // Arrange
        var factory = fixture.CreateFactory();
        var handler = new CreateProviderHandler(factory);

        var command = new CreateProviderCommand(
            Name: "Blofin",
            Type: "Exchange",
            ConnectorKind: "Syncable",
            DefaultCurrency: "USDT",
            SyncFrequency: "Daily",
            ConfigSchema: null);

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe("Blofin");
        result.Value.Type.ShouldBe("Exchange");
        result.Value.ConnectorKind.ShouldBe("Syncable");
        result.Value.DefaultCurrency.ShouldBe("USDT");
        result.Value.SyncFrequency.ShouldBe("Daily");
    }

    [Fact]
    public async Task HandleAsync_DuplicateName_ReturnsFailure()
    {
        // Arrange
        var factory = fixture.CreateFactory();
        var handler = new CreateProviderHandler(factory);

        var command = new CreateProviderCommand(
            Name: "DuplicateProvider",
            Type: "Manual",
            ConnectorKind: "Recurring",
            DefaultCurrency: "USD",
            SyncFrequency: "Manual",
            ConfigSchema: null);

        // Create first provider
        await handler.HandleAsync(command);

        // Act - try to create duplicate
        var handler2 = new CreateProviderHandler(factory);
        var result = await handler2.HandleAsync(command);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors[0].Message.ShouldContain("already exists");
    }

    [Fact]
    public async Task HandleAsync_InvalidType_ReturnsFailure()
    {
        // Arrange
        var factory = fixture.CreateFactory();
        var handler = new CreateProviderHandler(factory);

        var command = new CreateProviderCommand(
            Name: "TestProvider",
            Type: "InvalidType",
            ConnectorKind: "Recurring",
            DefaultCurrency: "USD",
            SyncFrequency: "Daily",
            ConfigSchema: null);

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors[0].Message.ShouldContain("Invalid provider type");
    }
}
