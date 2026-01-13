using System.Text.Json.Serialization;

namespace Connectors.Toobit.Models;

/// <summary>
/// Credentials required for Toobit API authentication.
/// </summary>
public sealed record ToobitCredentials
{
    [JsonPropertyName("apiKey")]
    public required string ApiKey { get; init; }

    [JsonPropertyName("secretKey")]
    public required string SecretKey { get; init; }

    public bool IsValid() =>
        !string.IsNullOrWhiteSpace(ApiKey) &&
        !string.IsNullOrWhiteSpace(SecretKey);
}
