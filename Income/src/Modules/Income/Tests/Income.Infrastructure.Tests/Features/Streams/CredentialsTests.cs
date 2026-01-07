using Income.Contracts.Commands;
using Income.Infrastructure.Features.Providers.Handlers;
using Income.Infrastructure.Features.Streams.Handlers;
using Income.Infrastructure.Services;
using Income.Infrastructure.Tests.Fixtures;

namespace Income.Infrastructure.Tests.Features.Streams;

[Collection("Postgres")]
public class CredentialsTests(PostgresFixture fixture)
{
    [Fact]
    public async Task CreateStream_WithCredentials_EncryptsAndStores()
    {
        // Arrange
        var providerId = await CreateProviderAsync();
        var credentials = """{"apiKey":"my-api-key","apiSecret":"my-secret"}""";

        var factory = fixture.CreateFactory();
        var encryptor = TestCredentialEncryptor.Create();
        var handler = new CreateStreamHandler(factory, encryptor);

        // Act
        var result = await handler.HandleAsync(new CreateStreamCommand(
            ProviderId: providerId,
            Name: "Trading Account with Creds",
            Category: "Trading",
            OriginalCurrency: "USDT",
            IsFixed: false,
            FixedPeriod: null,
            Credentials: credentials));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.HasCredentials.ShouldBeTrue();

        // Verify credentials are stored encrypted (not plaintext)
        await using var verifyContext = fixture.CreateDbContext();
        var entity = await verifyContext.Streams.FindAsync(result.Value.Id);
        entity.ShouldNotBeNull();
        entity!.EncryptedCredentials.ShouldNotBeNull();
        entity.EncryptedCredentials.ShouldNotBe(credentials); // Should be encrypted
    }

    [Fact]
    public async Task CreateStream_WithoutCredentials_HasCredentialsFalse()
    {
        // Arrange
        var providerId = await CreateProviderAsync();

        var factory = fixture.CreateFactory();
        var handler = new CreateStreamHandler(factory, TestCredentialEncryptor.Create());

        // Act
        var result = await handler.HandleAsync(new CreateStreamCommand(
            ProviderId: providerId,
            Name: "Stream Without Creds",
            Category: "Salary",
            OriginalCurrency: "USD",
            IsFixed: true,
            FixedPeriod: "Monthly"));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.HasCredentials.ShouldBeFalse();
    }

    [Fact]
    public async Task UpdateCredentials_AddsCredentialsToExistingStream()
    {
        // Arrange - Create stream without credentials
        var providerId = await CreateProviderAsync();
        var credentials = """{"apiKey":"updated-key","apiSecret":"updated-secret"}""";

        var factory = fixture.CreateFactory();
        var createHandler = new CreateStreamHandler(factory, TestCredentialEncryptor.Create());

        var createResult = await createHandler.HandleAsync(new CreateStreamCommand(
            ProviderId: providerId,
            Name: "Stream to Update",
            Category: "Trading",
            OriginalCurrency: "USDT",
            IsFixed: false,
            FixedPeriod: null));

        createResult.Value.HasCredentials.ShouldBeFalse();

        // Act - Update credentials
        var updateHandler = new UpdateCredentialsHandler(factory, TestCredentialEncryptor.Create());

        var updateResult = await updateHandler.HandleAsync(new UpdateCredentialsCommand(
            StreamId: createResult.Value.Id,
            Credentials: credentials));

        // Assert
        updateResult.IsSuccess.ShouldBeTrue();

        // Verify stream now has credentials
        await using var verifyContext = fixture.CreateDbContext();
        var entity = await verifyContext.Streams.FindAsync(createResult.Value.Id);
        entity.ShouldNotBeNull();
        entity!.EncryptedCredentials.ShouldNotBeNull();
    }

    [Fact]
    public void CredentialEncryptor_EncryptAndDecrypt_RoundTrips()
    {
        // Arrange
        var encryptor = new AesCredentialEncryptor("TestKey123!");
        var original = """{"apiKey":"test-key","apiSecret":"super-secret-value"}""";

        // Act
        var encrypted = encryptor.Encrypt(original);
        var decrypted = encryptor.Decrypt(encrypted);

        // Assert
        encrypted.ShouldNotBe(original);
        encrypted.ShouldNotBeEmpty();
        decrypted.ShouldBe(original);
    }

    private async Task<string> CreateProviderAsync()
    {
        var uniqueSuffix = Guid.NewGuid().ToString()[..8];

        var factory = fixture.CreateFactory();
        var handler = new CreateProviderHandler(factory);

        var result = await handler.HandleAsync(new CreateProviderCommand(
            Name: $"CredTestProvider_{uniqueSuffix}",
            Type: "Exchange",
            DefaultCurrency: "USDT",
            SyncFrequency: "Daily",
            ConfigSchema: """{"properties":{"apiKey":{"type":"string"},"apiSecret":{"type":"string"}}}"""));

        return result.Value.Id;
    }
}
