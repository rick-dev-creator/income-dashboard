using System.Text.Json.Serialization;

namespace Connectors.Blofin.Models;

/// <summary>
/// Credentials required for Blofin API authentication.
/// </summary>
public sealed record BlofinCredentials
{
    [JsonPropertyName("apiKey")]
    public required string ApiKey { get; init; }

    [JsonPropertyName("secretKey")]
    public required string SecretKey { get; init; }

    [JsonPropertyName("passphrase")]
    public required string Passphrase { get; init; }

    [JsonPropertyName("isDemo")]
    public bool IsDemo { get; init; } = false;

    public bool IsValid() =>
        !string.IsNullOrWhiteSpace(ApiKey) &&
        !string.IsNullOrWhiteSpace(SecretKey) &&
        !string.IsNullOrWhiteSpace(Passphrase);
}
