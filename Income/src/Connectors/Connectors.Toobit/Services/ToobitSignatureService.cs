using System.Security.Cryptography;
using System.Text;

namespace Connectors.Toobit.Services;

/// <summary>
/// Handles Toobit API authentication signature generation.
/// </summary>
internal static class ToobitSignatureService
{
    public const string BaseUrl = "https://api.toobit.com";

    /// <summary>
    /// Creates the HMAC-SHA256 signature required for Toobit API authentication.
    /// </summary>
    /// <param name="secretKey">Secret Key provided by Toobit</param>
    /// <param name="queryString">Query string parameters including timestamp</param>
    /// <returns>Hex-encoded signature</returns>
    public static string CreateSignature(string secretKey, string queryString)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(queryString));
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
    }

    /// <summary>
    /// Generates a timestamp in milliseconds since epoch.
    /// </summary>
    public static long GenerateTimestamp() =>
        DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    /// <summary>
    /// Builds the full query string with timestamp and signature.
    /// </summary>
    /// <param name="secretKey">Secret key for signing</param>
    /// <param name="additionalParams">Optional additional query parameters</param>
    /// <returns>Complete query string with timestamp and signature</returns>
    public static string BuildSignedQueryString(string secretKey, string? additionalParams = null)
    {
        var timestamp = GenerateTimestamp();
        var queryString = string.IsNullOrEmpty(additionalParams)
            ? $"timestamp={timestamp}"
            : $"{additionalParams}&timestamp={timestamp}";

        var signature = CreateSignature(secretKey, queryString);
        return $"{queryString}&signature={signature}";
    }
}
