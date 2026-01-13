using System.Text.Json.Serialization;

namespace Connectors.Toobit.Models;

/// <summary>
/// Account information response from /api/v1/account endpoint.
/// </summary>
internal sealed class ToobitAccountResponse
{
    [JsonPropertyName("balances")]
    public List<ToobitBalance> Balances { get; init; } = [];
}

/// <summary>
/// Individual asset balance from Toobit API.
/// </summary>
internal sealed class ToobitBalance
{
    [JsonPropertyName("asset")]
    public string Asset { get; init; } = string.Empty;

    [JsonPropertyName("assetId")]
    public string AssetId { get; init; } = string.Empty;

    [JsonPropertyName("assetName")]
    public string AssetName { get; init; } = string.Empty;

    [JsonPropertyName("total")]
    public string Total { get; init; } = "0";

    [JsonPropertyName("free")]
    public string Free { get; init; } = "0";

    [JsonPropertyName("locked")]
    public string Locked { get; init; } = "0";

    public decimal TotalAsDecimal =>
        decimal.TryParse(Total, out var result) ? result : 0;

    public decimal FreeAsDecimal =>
        decimal.TryParse(Free, out var result) ? result : 0;
}

/// <summary>
/// Error response from Toobit API.
/// </summary>
internal sealed class ToobitErrorResponse
{
    [JsonPropertyName("code")]
    public int Code { get; init; }

    [JsonPropertyName("msg")]
    public string Message { get; init; } = string.Empty;
}

/// <summary>
/// Ticker price response for USD conversion.
/// </summary>
internal sealed class ToobitTickerPrice
{
    [JsonPropertyName("symbol")]
    public string Symbol { get; init; } = string.Empty;

    [JsonPropertyName("price")]
    public string Price { get; init; } = "0";

    public decimal PriceAsDecimal =>
        decimal.TryParse(Price, out var result) ? result : 0;
}
