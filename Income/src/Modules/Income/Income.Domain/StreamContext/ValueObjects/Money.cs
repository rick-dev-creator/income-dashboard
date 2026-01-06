using FluentResults;

namespace Income.Domain.StreamContext.ValueObjects;

internal readonly record struct Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Result<Money> Create(decimal amount, string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
            return Result.Fail<Money>("Currency is required");

        if (currency.Length is < 2 or > 5)
            return Result.Fail<Money>("Currency must be between 2 and 5 characters");

        return Result.Ok(new Money(amount, currency.ToUpperInvariant()));
    }

    public static Money Usd(decimal amount) => new(amount, "USD");
    public static Money Zero(string currency) => new(0, currency.ToUpperInvariant());

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Cannot add {Currency} and {other.Currency}");
        return new(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Cannot subtract {Currency} and {other.Currency}");
        return new(Amount - other.Amount, Currency);
    }

    public Money Negate() => new(-Amount, Currency);
    public Money Abs() => new(Math.Abs(Amount), Currency);

    public bool IsZero => Amount == 0;
    public bool IsPositive => Amount > 0;
    public bool IsNegative => Amount < 0;

    public override string ToString() => $"{Amount:N2} {Currency}";
}

internal static class MoneyExtensions
{
    extension(Money money)
    {
        public Money ConvertTo(string targetCurrency, decimal exchangeRate)
        {
            if (money.Currency == targetCurrency)
                return money;

            var convertedAmount = money.Amount * exchangeRate;
            return Money.Create(convertedAmount, targetCurrency).Value;
        }

        public Money ToUsd(decimal exchangeRate) => money.ConvertTo("USD", exchangeRate);
    }

    extension(Money)
    {
        public static Money operator +(Money left, Money right) => left.Add(right);
        public static Money operator -(Money left, Money right) => left.Subtract(right);
        public static Money operator -(Money money) => money.Negate();
    }
}
