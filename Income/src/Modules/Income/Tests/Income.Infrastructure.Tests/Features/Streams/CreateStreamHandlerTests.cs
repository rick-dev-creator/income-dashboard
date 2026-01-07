using Income.Contracts.Commands;
using Income.Infrastructure.Features.Providers.Handlers;
using Income.Infrastructure.Features.Streams.Handlers;
using Income.Infrastructure.Tests.Fixtures;

namespace Income.Infrastructure.Tests.Features.Streams;

[Collection("Postgres")]
public class CreateStreamHandlerTests(PostgresFixture fixture)
{
    [Fact]
    public async Task HandleAsync_ValidCommand_CreatesStream()
    {
        // Arrange - Create a provider first
        var factory = fixture.CreateFactory();
        var createProviderHandler = new CreateProviderHandler(factory);

        var uniqueSuffix = Guid.NewGuid().ToString()[..8];
        var providerResult = await createProviderHandler.HandleAsync(new CreateProviderCommand(
            Name: $"TestProvider_{uniqueSuffix}",
            Type: "Exchange",
            DefaultCurrency: "USDT",
            SyncFrequency: "Daily",
            ConfigSchema: null));

        // Act
        var handler = new CreateStreamHandler(factory, TestCredentialEncryptor.Create());

        var command = new CreateStreamCommand(
            ProviderId: providerResult.Value.Id,
            Name: "Trading Account",
            Category: "Trading",
            OriginalCurrency: "USDT",
            IsFixed: false,
            FixedPeriod: null);

        var result = await handler.HandleAsync(command);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe("Trading Account");
        result.Value.Category.ShouldBe("Trading");
        result.Value.OriginalCurrency.ShouldBe("USDT");
        result.Value.IsFixed.ShouldBeFalse();
    }

    [Fact]
    public async Task HandleAsync_InvalidProvider_ReturnsFailure()
    {
        // Arrange
        var factory = fixture.CreateFactory();
        var handler = new CreateStreamHandler(factory, TestCredentialEncryptor.Create());

        var command = new CreateStreamCommand(
            ProviderId: "non-existent-provider",
            Name: "Test Stream",
            Category: "Trading",
            OriginalCurrency: "USD",
            IsFixed: false,
            FixedPeriod: null);

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors[0].Message.ShouldContain("not found");
    }

    [Fact]
    public async Task HandleAsync_InvalidCategory_ReturnsFailure()
    {
        // Arrange - Create a provider first
        var factory = fixture.CreateFactory();
        var createProviderHandler = new CreateProviderHandler(factory);

        var uniqueSuffix = Guid.NewGuid().ToString()[..8];
        var providerResult = await createProviderHandler.HandleAsync(new CreateProviderCommand(
            Name: $"CategoryTestProvider_{uniqueSuffix}",
            Type: "Manual",
            DefaultCurrency: "USD",
            SyncFrequency: "Manual",
            ConfigSchema: null));

        // Act
        var handler = new CreateStreamHandler(factory, TestCredentialEncryptor.Create());

        var command = new CreateStreamCommand(
            ProviderId: providerResult.Value.Id,
            Name: "Test Stream",
            Category: "InvalidCategory",
            OriginalCurrency: "USD",
            IsFixed: false,
            FixedPeriod: null);

        var result = await handler.HandleAsync(command);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors[0].Message.ShouldContain("Invalid category");
    }
}
