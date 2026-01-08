using System.Text.Json.Serialization;

namespace Connectors.Blofin.Models;

/// <summary>
/// Base response wrapper for all Blofin API responses.
/// </summary>
internal sealed class BlofinApiResponse<T>
{
    [JsonPropertyName("code")]
    public string Code { get; init; } = string.Empty;

    [JsonPropertyName("msg")]
    public string Message { get; init; } = string.Empty;

    [JsonPropertyName("data")]
    public T? Data { get; init; }

    public bool IsSuccess => Code == "0";
}

/// <summary>
/// Account balance information from Blofin API.
/// </summary>
internal sealed class BlofinBalance
{
    [JsonPropertyName("currency")]
    public string Currency { get; init; } = string.Empty;

    [JsonPropertyName("balance")]
    public string Balance { get; init; } = "0";

    [JsonPropertyName("available")]
    public string Available { get; init; } = "0";

    [JsonPropertyName("frozen")]
    public string Frozen { get; init; } = "0";

    [JsonPropertyName("bonus")]
    public string Bonus { get; init; } = "0";

    public decimal BalanceAsDecimal =>
        decimal.TryParse(Balance, out var result) ? result : 0;

    public decimal AvailableAsDecimal =>
        decimal.TryParse(Available, out var result) ? result : 0;
}

/// <summary>
/// Supported Blofin account types for balance queries.
/// </summary>
internal static class BlofinAccountTypes
{
    public const string Funding = "funding";
    public const string Futures = "futures";
    public const string Spot = "spot";
    public const string CopyTrading = "copy_trading";
    public const string Earn = "earn";

    public static readonly string[] All = [Funding, Futures, Spot, CopyTrading, Earn];
}
