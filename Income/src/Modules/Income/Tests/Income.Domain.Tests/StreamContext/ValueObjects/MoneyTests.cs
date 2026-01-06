namespace Income.Domain.Tests.StreamContext.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        // Arrange & Act
        var result = Money.Create(100.50m, "USD");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Amount.ShouldBe(100.50m);
        result.Value.Currency.ShouldBe("USD");
    }

    [Fact]
    public void Create_WithLowercaseCurrency_ShouldNormalize()
    {
        // Arrange & Act
        var result = Money.Create(50m, "usd");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Currency.ShouldBe("USD");
    }

    [Fact]
    public void Create_WithEmptyCurrency_ShouldFail()
    {
        // Arrange & Act
        var result = Money.Create(100m, "");

        // Assert
        result.IsFailed.ShouldBeTrue();
    }

    [Fact]
    public void Add_SameCurrency_ShouldSucceed()
    {
        // Arrange
        var money1 = Money.Create(100m, "USD").Value;
        var money2 = Money.Create(50m, "USD").Value;

        // Act
        var result = money1.Add(money2);

        // Assert
        result.Amount.ShouldBe(150m);
        result.Currency.ShouldBe("USD");
    }

    [Fact]
    public void Add_DifferentCurrency_ShouldThrow()
    {
        // Arrange
        var money1 = Money.Create(100m, "USD").Value;
        var money2 = Money.Create(50m, "JPY").Value;

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => money1.Add(money2));
    }

    [Fact]
    public void Subtract_SameCurrency_ShouldSucceed()
    {
        // Arrange
        var money1 = Money.Create(100m, "USD").Value;
        var money2 = Money.Create(30m, "USD").Value;

        // Act
        var result = money1.Subtract(money2);

        // Assert
        result.Amount.ShouldBe(70m);
    }

    [Fact]
    public void IsZero_WithZeroAmount_ShouldBeTrue()
    {
        // Arrange
        var money = Money.Zero("USD");

        // Assert
        money.IsZero.ShouldBeTrue();
    }

    [Fact]
    public void IsPositive_WithPositiveAmount_ShouldBeTrue()
    {
        // Arrange
        var money = Money.Create(100m, "USD").Value;

        // Assert
        money.IsPositive.ShouldBeTrue();
        money.IsNegative.ShouldBeFalse();
    }

    [Fact]
    public void Negate_ShouldReturnNegativeAmount()
    {
        // Arrange
        var money = Money.Create(100m, "USD").Value;

        // Act
        var negated = money.Negate();

        // Assert
        negated.Amount.ShouldBe(-100m);
        negated.IsNegative.ShouldBeTrue();
    }

    [Fact]
    public void Usd_ShouldCreateUsdMoney()
    {
        // Arrange & Act
        var money = Money.Usd(250m);

        // Assert
        money.Amount.ShouldBe(250m);
        money.Currency.ShouldBe("USD");
    }
}
