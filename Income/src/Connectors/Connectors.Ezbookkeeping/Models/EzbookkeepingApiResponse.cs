using System.Text.Json.Serialization;

namespace Connectors.Ezbookkeeping.Models;

/// <summary>
/// Base response envelope for all ezbookkeeping API responses.
/// </summary>
internal sealed class EzbookkeepingApiResponse<T>
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("result")]
    public T? Result { get; init; }

    [JsonPropertyName("errorCode")]
    public string? ErrorCode { get; init; }

    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Transaction from ezbookkeeping API.
/// </summary>
internal sealed class EzbookkeepingTransaction
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("type")]
    public int Type { get; init; }

    [JsonPropertyName("time")]
    public long Time { get; init; }

    [JsonPropertyName("sourceAmount")]
    public long SourceAmount { get; init; }

    [JsonPropertyName("sourceAccount")]
    public EzbookkeepingAccount? SourceAccount { get; init; }

    [JsonPropertyName("comment")]
    public string? Comment { get; init; }

    /// <summary>
    /// Convert the integer amount to decimal (amounts have 2 implicit decimal places).
    /// </summary>
    public decimal AmountAsDecimal => SourceAmount / 100m;

    /// <summary>
    /// Transaction date derived from unix timestamp.
    /// </summary>
    public DateOnly Date => DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeSeconds(Time).UtcDateTime);
}

/// <summary>
/// ezbookkeeping transaction types.
/// </summary>
internal static class EzbookkeepingTransactionTypes
{
    public const int BalanceModification = 1;
    public const int Income = 2;
    public const int Expense = 3;
    public const int Transfer = 4;
}

/// <summary>
/// Account information from ezbookkeeping API.
/// </summary>
internal sealed class EzbookkeepingAccount
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("currency")]
    public string Currency { get; init; } = string.Empty;
}

/// <summary>
/// User profile from ezbookkeeping API (used for credential validation).
/// </summary>
internal sealed class EzbookkeepingUserProfile
{
    [JsonPropertyName("username")]
    public string Username { get; init; } = string.Empty;

    [JsonPropertyName("defaultCurrency")]
    public string DefaultCurrency { get; init; } = string.Empty;
}

/// <summary>
/// Exchange rate data from ezbookkeeping API.
/// </summary>
internal sealed class EzbookkeepingExchangeRates
{
    [JsonPropertyName("dataSource")]
    public string DataSource { get; init; } = string.Empty;

    [JsonPropertyName("baseCurrency")]
    public string BaseCurrency { get; init; } = string.Empty;

    [JsonPropertyName("exchangeRates")]
    public List<EzbookkeepingRate> ExchangeRates { get; init; } = [];
}

internal sealed class EzbookkeepingRate
{
    [JsonPropertyName("currency")]
    public string Currency { get; init; } = string.Empty;

    [JsonPropertyName("rate")]
    public string Rate { get; init; } = "0";

    public decimal RateAsDecimal =>
        decimal.TryParse(Rate, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var result) ? result : 0;
}
