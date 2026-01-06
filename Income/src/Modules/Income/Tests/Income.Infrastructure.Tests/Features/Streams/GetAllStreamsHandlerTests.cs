using Income.Contracts.Commands;
using Income.Contracts.Queries;
using Income.Infrastructure.Features.Providers.Handlers;
using Income.Infrastructure.Features.Streams.Handlers;
using Income.Infrastructure.Tests.Fixtures;

namespace Income.Infrastructure.Tests.Features.Streams;

[Collection("Postgres")]
public class GetAllStreamsHandlerTests(PostgresFixture fixture)
{
    [Fact]
    public async Task HandleAsync_WithStreams_ReturnsAll()
    {
        // Arrange - Create provider and streams
        var uniqueSuffix = Guid.NewGuid().ToString()[..8];
        var providerId = await CreateProviderAsync(uniqueSuffix);

        await using var createContext = fixture.CreateDbContext();
        var createHandler = new CreateStreamHandler(createContext, TestCredentialEncryptor.Create());

        await createHandler.HandleAsync(new CreateStreamCommand(
            ProviderId: providerId,
            Name: $"Stream1_{uniqueSuffix}",
            Category: "Trading",
            OriginalCurrency: "USDT",
            IsFixed: false,
            FixedPeriod: null));

        await createHandler.HandleAsync(new CreateStreamCommand(
            ProviderId: providerId,
            Name: $"Stream2_{uniqueSuffix}",
            Category: "Salary",
            OriginalCurrency: "USD",
            IsFixed: true,
            FixedPeriod: "Monthly"));

        // Act
        await using var context = fixture.CreateDbContext();
        var handler = new GetAllStreamsHandler(context);

        var result = await handler.HandleAsync(new GetAllStreamsQuery());

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBeGreaterThanOrEqualTo(2);
    }

    private async Task<string> CreateProviderAsync(string suffix)
    {
        await using var context = fixture.CreateDbContext();
        var handler = new CreateProviderHandler(context);

        var result = await handler.HandleAsync(new CreateProviderCommand(
            Name: $"GetAllStreamsTestProvider_{suffix}",
            Type: "Exchange",
            DefaultCurrency: "USDT",
            SyncFrequency: "Daily",
            ConfigSchema: null));

        return result.Value.Id;
    }
}
