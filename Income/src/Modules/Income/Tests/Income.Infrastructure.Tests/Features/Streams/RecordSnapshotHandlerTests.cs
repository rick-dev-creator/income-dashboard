using Income.Contracts.Commands;
using Income.Infrastructure.Features.Providers.Handlers;
using Income.Infrastructure.Features.Streams.Handlers;
using Income.Infrastructure.Tests.Fixtures;

namespace Income.Infrastructure.Tests.Features.Streams;

[Collection("Postgres")]
public class RecordSnapshotHandlerTests(PostgresFixture fixture)
{
    [Fact]
    public async Task HandleAsync_ValidCommand_RecordsSnapshot()
    {
        // Arrange - Create provider and stream
        var (providerId, streamId) = await CreateProviderAndStreamAsync();

        var factory = fixture.CreateFactory();
        var handler = new RecordSnapshotHandler(factory);

        var command = new RecordSnapshotCommand(
            StreamId: streamId,
            Date: DateOnly.FromDateTime(DateTime.UtcNow),
            OriginalAmount: 1000m,
            OriginalCurrency: "USDT",
            UsdAmount: 1000m,
            ExchangeRate: 1m,
            RateSource: "Manual");

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.OriginalAmount.ShouldBe(1000m);
        result.Value.UsdAmount.ShouldBe(1000m);
        result.Value.ExchangeRate.ShouldBe(1m);
    }

    [Fact]
    public async Task HandleAsync_SameDateTwice_UpdatesSnapshot()
    {
        // Arrange - Create provider and stream
        var (providerId, streamId) = await CreateProviderAndStreamAsync();
        var date = DateOnly.FromDateTime(DateTime.UtcNow);

        // Record first snapshot
        var factory = fixture.CreateFactory();
        var handler1 = new RecordSnapshotHandler(factory);

        await handler1.HandleAsync(new RecordSnapshotCommand(
            StreamId: streamId,
            Date: date,
            OriginalAmount: 1000m,
            OriginalCurrency: "USDT",
            UsdAmount: 1000m,
            ExchangeRate: 1m,
            RateSource: "Manual"));

        // Act - Record second snapshot for same date
        var handler2 = new RecordSnapshotHandler(factory);

        var result = await handler2.HandleAsync(new RecordSnapshotCommand(
            StreamId: streamId,
            Date: date,
            OriginalAmount: 2000m,
            OriginalCurrency: "USDT",
            UsdAmount: 2000m,
            ExchangeRate: 1m,
            RateSource: "Manual"));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.OriginalAmount.ShouldBe(2000m);
        result.Value.UsdAmount.ShouldBe(2000m);
    }

    [Fact]
    public async Task HandleAsync_InvalidStreamId_ReturnsFailure()
    {
        // Arrange
        var factory = fixture.CreateFactory();
        var handler = new RecordSnapshotHandler(factory);

        var command = new RecordSnapshotCommand(
            StreamId: "non-existent-stream",
            Date: DateOnly.FromDateTime(DateTime.UtcNow),
            OriginalAmount: 1000m,
            OriginalCurrency: "USDT",
            UsdAmount: 1000m,
            ExchangeRate: 1m,
            RateSource: "Manual");

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors[0].Message.ShouldContain("not found");
    }

    private async Task<(string ProviderId, string StreamId)> CreateProviderAndStreamAsync()
    {
        var uniqueSuffix = Guid.NewGuid().ToString()[..8];
        var factory = fixture.CreateFactory();

        // Create provider
        var createProviderHandler = new CreateProviderHandler(factory);

        var providerResult = await createProviderHandler.HandleAsync(new CreateProviderCommand(
            Name: $"SnapshotTestProvider_{uniqueSuffix}",
            Type: "Exchange",
            DefaultCurrency: "USDT",
            SyncFrequency: "Daily",
            ConfigSchema: null));

        // Create stream
        var createStreamHandler = new CreateStreamHandler(factory, TestCredentialEncryptor.Create());

        var streamResult = await createStreamHandler.HandleAsync(new CreateStreamCommand(
            ProviderId: providerResult.Value.Id,
            Name: $"TestStream_{uniqueSuffix}",
            Category: "Trading",
            OriginalCurrency: "USDT",
            IsFixed: false,
            FixedPeriod: null));

        return (providerResult.Value.Id, streamResult.Value.Id);
    }
}
