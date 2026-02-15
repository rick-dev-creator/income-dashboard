using System.Text.Json.Serialization;

namespace Connectors.Ezbookkeeping.Models;

/// <summary>
/// Credentials required for ezbookkeeping API authentication.
/// </summary>
public sealed record EzbookkeepingCredentials
{
    [JsonPropertyName("serverUrl")]
    public required string ServerUrl { get; init; }

    [JsonPropertyName("apiToken")]
    public required string ApiToken { get; init; }

    /// <summary>
    /// Determines which transactions to fetch: "income" or "expense".
    /// Set automatically based on the stream type when the stream is created.
    /// </summary>
    [JsonPropertyName("streamMode")]
    public string StreamMode { get; init; } = "income";

    public bool IsIncomeMode => StreamMode.Equals("income", StringComparison.OrdinalIgnoreCase);

    public bool IsValid() =>
        !string.IsNullOrWhiteSpace(ServerUrl) &&
        !string.IsNullOrWhiteSpace(ApiToken);

    /// <summary>
    /// Returns the base API URL normalized (no trailing slash).
    /// </summary>
    public string GetBaseUrl()
    {
        var url = ServerUrl.TrimEnd('/');
        return url;
    }
}
