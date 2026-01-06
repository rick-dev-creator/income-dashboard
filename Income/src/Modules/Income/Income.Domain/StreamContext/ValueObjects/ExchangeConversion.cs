namespace Income.Domain.StreamContext.ValueObjects;

internal sealed record ExchangeConversion(
    Money OriginalMoney,
    Money UsdMoney,
    decimal Rate,
    string Source,
    DateTime ConvertedAt)
{
    public bool IsConverted => OriginalMoney.Currency != "USD";

    public static ExchangeConversion NoConversion(Money usdMoney) =>
        new(usdMoney, usdMoney, 1m, "None", DateTime.UtcNow);

    public static ExchangeConversion Create(
        Money original,
        decimal rateToUsd,
        string source)
    {
        var usdAmount = original.Amount * rateToUsd;
        var usdMoney = Money.Usd(usdAmount);
        return new(original, usdMoney, rateToUsd, source, DateTime.UtcNow);
    }
}
