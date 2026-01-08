using System.Security.Cryptography;
using System.Text;
using Connectors.Blofin.Models;

namespace Connectors.Blofin.Services;

/// <summary>
/// Handles Blofin API authentication signature generation.
/// </summary>
internal sealed class BlofinSignatureService
{
    /// <summary>
    /// Creates the HMAC-SHA256 signature required for Blofin API authentication.
    /// </summary>
    /// <param name="secretKey">Secret Key provided by Blofin</param>
    /// <param name="requestPath">Path including query parameters for GET requests</param>
    /// <param name="method">HTTP method in uppercase (GET, POST)</param>
    /// <param name="timestamp">UTC timestamp in milliseconds</param>
    /// <param name="nonce">Unique identifier (UUID)</param>
    /// <param name="body">JSON body for POST requests, null for GET</param>
    /// <returns>Base64-encoded signature</returns>
    public static string CreateSignature(
        string secretKey,
        string requestPath,
        string method,
        string timestamp,
        string nonce,
        string? body = null)
    {
        // Concatenate: path + method + timestamp + nonce + body
        var prehashString = $"{requestPath}{method}{timestamp}{nonce}{body ?? string.Empty}";

        // Generate HMAC-SHA256 signature
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(prehashString));

        // Convert to hexadecimal string (lowercase)
        var hexSignature = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

        // Base64 encode the hex string bytes (Blofin specific)
        var hexBytes = Encoding.UTF8.GetBytes(hexSignature);
        return Convert.ToBase64String(hexBytes);
    }

    /// <summary>
    /// Generates a timestamp in milliseconds since epoch.
    /// </summary>
    public static string GenerateTimestamp() =>
        DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

    /// <summary>
    /// Generates a unique nonce (UUID).
    /// </summary>
    public static string GenerateNonce() =>
        Guid.NewGuid().ToString();

    /// <summary>
    /// Creates all required authentication headers for a Blofin API request.
    /// </summary>
    public static Dictionary<string, string> CreateAuthHeaders(
        BlofinCredentials credentials,
        string requestPath,
        string method,
        string? body = null)
    {
        var timestamp = GenerateTimestamp();
        var nonce = GenerateNonce();
        var signature = CreateSignature(
            credentials.SecretKey,
            requestPath,
            method,
            timestamp,
            nonce,
            body);

        return new Dictionary<string, string>
        {
            ["ACCESS-KEY"] = credentials.ApiKey,
            ["ACCESS-SIGN"] = signature,
            ["ACCESS-TIMESTAMP"] = timestamp,
            ["ACCESS-NONCE"] = nonce,
            ["ACCESS-PASSPHRASE"] = credentials.Passphrase
        };
    }

    /// <summary>
    /// Gets the base URL based on environment (demo vs production).
    /// </summary>
    public static string GetBaseUrl(bool isDemo) =>
        isDemo
            ? "https://demo-trading-openapi.blofin.com"
            : "https://openapi.blofin.com";
}
