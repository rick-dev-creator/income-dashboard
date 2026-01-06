using Income.Contracts.Commands;
using Income.Contracts.Queries;
using Income.Infrastructure.Features.Providers.Handlers;
using Income.Infrastructure.Features.Streams.Handlers;
using Income.Infrastructure.Tests.Fixtures;

namespace Income.Infrastructure.Tests.Features.Streams;

[Collection("Postgres")]
public class GetStreamHandlerTests(PostgresFixture fixture)
{
    [Fact]
    public async Task HandleAsync_ExistingStream_ReturnsStream()
    {
        // Arrange - Create provider and stream
        var streamId = await CreateStreamAsync();

        await using var context = fixture.CreateDbContext();
        var handler = new GetStreamHandler(context);

        // Act
        var result = await handler.HandleAsync(new GetStreamQuery(streamId));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldBe(streamId);
    }

    [Fact]
    public async Task HandleAsync_NonExistingStream_ReturnsFailure()
    {
        // Arrange
        await using var context = fixture.CreateDbContext();
        var handler = new GetStreamHandler(context);

        // Act
        var result = await handler.HandleAsync(new GetStreamQuery("non-existent-id"));

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors[0].Message.ShouldContain("not found");
    }

    [Fact]
    public async Task HandleAsync_StreamWithSnapshots_IncludesSnapshots()
    {
        // Arrange - Create provider, stream and snapshot
        var streamId = await CreateStreamAsync();

        // Add snapshot
        await using var snapshotContext = fixture.CreateDbContext();
        var snapshotHandler = new RecordSnapshotHandler(snapshotContext);

        await snapshotHandler.HandleAsync(new RecordSnapshotCommand(
            StreamId: streamId,
            Date: DateOnly.FromDateTime(DateTime.UtcNow),
            OriginalAmount: 5000m,
            OriginalCurrency: "USDT",
            UsdAmount: 5000m,
            ExchangeRate: 1m,
            RateSource: "Test"));

        // Act
        await using var context = fixture.CreateDbContext();
        var handler = new GetStreamHandler(context);

        var result = await handler.HandleAsync(new GetStreamQuery(streamId));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Snapshots.ShouldNotBeEmpty();
        result.Value.Snapshots[0].OriginalAmount.ShouldBe(5000m);
    }

    private async Task<string> CreateStreamAsync()
    {
        var uniqueSuffix = Guid.NewGuid().ToString()[..8];

        // Create provider
        await using var providerContext = fixture.CreateDbContext();
        var createProviderHandler = new CreateProviderHandler(providerContext);

        var providerResult = await createProviderHandler.HandleAsync(new CreateProviderCommand(
            Name: $"GetStreamTestProvider_{uniqueSuffix}",
            Type: "Exchange",
            DefaultCurrency: "USDT",
            SyncFrequency: "Daily",
            ConfigSchema: null));

        // Create stream
        await using var streamContext = fixture.CreateDbContext();
        var createStreamHandler = new CreateStreamHandler(streamContext, TestCredentialEncryptor.Create());

        var streamResult = await createStreamHandler.HandleAsync(new CreateStreamCommand(
            ProviderId: providerResult.Value.Id,
            Name: $"GetStreamTest_{uniqueSuffix}",
            Category: "Trading",
            OriginalCurrency: "USDT",
            IsFixed: false,
            FixedPeriod: null));

        return streamResult.Value.Id;
    }
}
