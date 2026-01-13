using System.Text.Json;
using Connectors.Toobit.Models;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace Connectors.Toobit.Services;

/// <summary>
/// HTTP client for Toobit API calls.
/// </summary>
internal sealed class ToobitApiClient(
    IHttpClientFactory httpClientFactory,
    ILogger<ToobitApiClient> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // Cache for ticker prices (refreshed each sync)
    private Dictionary<string, decimal> _priceCache = new();
    private DateTime _priceCacheTime = DateTime.MinValue;
    private static readonly TimeSpan PriceCacheExpiry = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Validates credentials by fetching account balance.
    /// </summary>
    public async Task<Result> ValidateCredentialsAsync(
        ToobitCredentials credentials,
        CancellationToken ct = default)
    {
        var result = await GetAccountBalancesAsync(credentials, ct);
        return result.IsSuccess ? Result.Ok() : Result.Fail(result.Errors);
    }

    /// <summary>
    /// Gets account balances from Toobit.
    /// </summary>
    public async Task<Result<ToobitAccountResponse>> GetAccountBalancesAsync(
        ToobitCredentials credentials,
        CancellationToken ct = default)
    {
        const string endpoint = "/api/v1/account";
        var queryString = ToobitSignatureService.BuildSignedQueryString(credentials.SecretKey);
        var fullPath = $"{endpoint}?{queryString}";

        return await SendAuthenticatedRequestAsync<ToobitAccountResponse>(
            credentials.ApiKey, fullPath, ct);
    }

    /// <summary>
    /// Gets ticker prices for USD conversion.
    /// </summary>
    public async Task<Result<List<ToobitTickerPrice>>> GetTickerPricesAsync(CancellationToken ct = default)
    {
        const string endpoint = "/quote/v1/ticker/price";

        try
        {
            var client = httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(ToobitSignatureService.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);

            var response = await client.GetAsync(endpoint, ct);
            var content = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Toobit ticker request failed: {StatusCode} - {Content}",
                    response.StatusCode, content);
                return Result.Fail($"Ticker request failed: {response.StatusCode}");
            }

            var prices = JsonSerializer.Deserialize<List<ToobitTickerPrice>>(content, JsonOptions);
            return prices is not null
                ? Result.Ok(prices)
                : Result.Fail("Failed to deserialize ticker prices");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching ticker prices");
            return Result.Fail($"Exception: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets total balance snapshot in USD equivalent.
    /// </summary>
    public async Task<Result<BalanceSnapshot>> GetBalanceSnapshotAsync(
        ToobitCredentials credentials,
        CancellationToken ct = default)
    {
        // Refresh price cache if needed
        await RefreshPriceCacheAsync(ct);

        // Get account balances
        var balancesResult = await GetAccountBalancesAsync(credentials, ct);
        if (balancesResult.IsFailed)
            return Result.Fail(balancesResult.Errors);

        var balances = balancesResult.Value.Balances;
        var totalUsd = 0m;
        var assetBalances = new Dictionary<string, decimal>();

        foreach (var balance in balances.Where(b => b.TotalAsDecimal > 0))
        {
            var amount = balance.TotalAsDecimal;
            var usdValue = ConvertToUsd(balance.Asset, amount);

            if (usdValue > 0)
            {
                assetBalances[balance.Asset] = usdValue;
                totalUsd += usdValue;

                logger.LogDebug("Asset {Asset}: {Amount} = ${Usd}",
                    balance.Asset, amount, usdValue);
            }
        }

        logger.LogInformation("Toobit balance snapshot: TotalUsd={TotalUsd}, Assets={Count}",
            totalUsd, assetBalances.Count);

        return Result.Ok(new BalanceSnapshot(
            TotalUsd: totalUsd,
            BalancesByAsset: assetBalances,
            SnapshotTime: DateTime.UtcNow));
    }

    private async Task RefreshPriceCacheAsync(CancellationToken ct)
    {
        if (DateTime.UtcNow - _priceCacheTime < PriceCacheExpiry)
            return;

        var result = await GetTickerPricesAsync(ct);
        if (result.IsSuccess)
        {
            _priceCache = result.Value
                .Where(p => p.PriceAsDecimal > 0)
                .ToDictionary(p => p.Symbol, p => p.PriceAsDecimal);
            _priceCacheTime = DateTime.UtcNow;

            logger.LogDebug("Price cache refreshed: {Count} symbols", _priceCache.Count);
        }
    }

    private decimal ConvertToUsd(string asset, decimal amount)
    {
        // USDT/USDC are considered 1:1 with USD
        if (asset.Equals("USDT", StringComparison.OrdinalIgnoreCase) ||
            asset.Equals("USDC", StringComparison.OrdinalIgnoreCase) ||
            asset.Equals("USD", StringComparison.OrdinalIgnoreCase))
        {
            return amount;
        }

        // Try to find a USDT pair for this asset
        var usdtPair = $"{asset}USDT";
        if (_priceCache.TryGetValue(usdtPair, out var price))
        {
            return amount * price;
        }

        // Try USDC pair
        var usdcPair = $"{asset}USDC";
        if (_priceCache.TryGetValue(usdcPair, out price))
        {
            return amount * price;
        }

        logger.LogWarning("No USD price found for asset: {Asset}", asset);
        return 0;
    }

    private async Task<Result<T>> SendAuthenticatedRequestAsync<T>(
        string apiKey,
        string fullPath,
        CancellationToken ct)
    {
        try
        {
            var client = httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(ToobitSignatureService.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);

            var request = new HttpRequestMessage(HttpMethod.Get, fullPath);
            request.Headers.Add("X-BB-APIKEY", apiKey);

            var response = await client.SendAsync(request, ct);
            var content = await response.Content.ReadAsStringAsync(ct);

            logger.LogDebug("Toobit API response: {StatusCode} - {Content}",
                response.StatusCode, content);

            if (!response.IsSuccessStatusCode)
            {
                // Try to parse error response
                var error = JsonSerializer.Deserialize<ToobitErrorResponse>(content, JsonOptions);
                var message = error?.Message ?? content;
                logger.LogWarning("Toobit API error: {Code} - {Message}",
                    error?.Code, message);
                return Result.Fail($"API error: {message}");
            }

            var data = JsonSerializer.Deserialize<T>(content, JsonOptions);
            if (data is null)
            {
                logger.LogWarning("Failed to deserialize Toobit response: {Content}", content);
                return Result.Fail("Failed to deserialize API response");
            }

            return Result.Ok(data);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error calling Toobit API: {Path}", fullPath);
            return Result.Fail($"Exception: {ex.Message}");
        }
    }
}

/// <summary>
/// Snapshot of account balances at a point in time.
/// </summary>
internal sealed record BalanceSnapshot(
    decimal TotalUsd,
    Dictionary<string, decimal> BalancesByAsset,
    DateTime SnapshotTime);
