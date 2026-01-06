namespace Income.Domain.Tests.ProviderContext.Aggregates;

public class ProviderTests
{
    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        // Act
        var result = Provider.Create(
            name: "Blofin",
            type: ProviderType.Exchange,
            defaultCurrency: "USDT",
            syncFrequency: SyncFrequency.Daily);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe("Blofin");
        result.Value.Type.ShouldBe(ProviderType.Exchange);
        result.Value.DefaultCurrency.ShouldBe("USDT");
    }

    [Fact]
    public void Create_WithEmptyName_ShouldFail()
    {
        // Act
        var result = Provider.Create(
            name: "",
            type: ProviderType.Exchange,
            defaultCurrency: "USD",
            syncFrequency: SyncFrequency.Daily);

        // Assert
        result.IsFailed.ShouldBeTrue();
    }

    [Fact]
    public void Create_WithLowercaseCurrency_ShouldNormalize()
    {
        // Act
        var result = Provider.Create(
            name: "Test",
            type: ProviderType.Manual,
            defaultCurrency: "usd",
            syncFrequency: SyncFrequency.Manual);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.DefaultCurrency.ShouldBe("USD");
    }

    [Fact]
    public void RequiresCredentials_ForExchange_ShouldBeTrue()
    {
        // Arrange
        var provider = Provider.Create(
            "Blofin", ProviderType.Exchange, "USD", SyncFrequency.Daily).Value;

        // Assert
        provider.RequiresCredentials.ShouldBeTrue();
    }

    [Fact]
    public void RequiresCredentials_ForManual_ShouldBeFalse()
    {
        // Arrange
        var provider = Provider.Create(
            "Salary", ProviderType.Manual, "USD", SyncFrequency.Manual).Value;

        // Assert
        provider.RequiresCredentials.ShouldBeFalse();
    }

    [Fact]
    public void SupportsAutoSync_ForDaily_ShouldBeTrue()
    {
        // Arrange
        var provider = Provider.Create(
            "Blofin", ProviderType.Exchange, "USD", SyncFrequency.Daily).Value;

        // Assert
        provider.SupportsAutoSync.ShouldBeTrue();
    }

    [Fact]
    public void SupportsAutoSync_ForManual_ShouldBeFalse()
    {
        // Arrange
        var provider = Provider.Create(
            "Salary", ProviderType.Manual, "USD", SyncFrequency.Manual).Value;

        // Assert
        provider.SupportsAutoSync.ShouldBeFalse();
    }
}
