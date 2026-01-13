using System.Text.Json;
using Connectors.Blofin.Models;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace Connectors.Blofin.Services;

/// <summary>
/// HTTP client for Blofin API calls.
/// </summary>
internal sealed class BlofinApiClient(
    IHttpClientFactory httpClientFactory,
    ILogger<BlofinApiClient> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Validates credentials by fetching account balance.
    /// </summary>
    public async Task<Result> ValidateCredentialsAsync(
        BlofinCredentials credentials,
        CancellationToken ct = default)
    {
        // Try to fetch futures account balance as a validation check
        var result = await GetFuturesAccountBalanceAsync(credentials, ct);

        return result.IsSuccess
            ? Result.Ok()
            : Result.Fail(result.Errors);
    }

    /// <summary>
    /// Gets the futures account balance with total equity in USD.
    /// This endpoint returns the total value of all assets converted to USD.
    /// </summary>
    public async Task<Result<BlofinAccountBalance>> GetFuturesAccountBalanceAsync(
        BlofinCredentials credentials,
        CancellationToken ct = default)
    {
        const string endpoint = "/api/v1/account/balance";
        return await SendRequestAsync<BlofinAccountBalance>(credentials, endpoint, ct);
    }

    /// <summary>
    /// Gets balances for a specific account type.
    /// </summary>
    public async Task<Result<List<BlofinBalance>>> GetAccountBalancesAsync(
        BlofinCredentials credentials,
        string accountType,
        CancellationToken ct = default)
    {
        var endpoint = $"/api/v1/asset/balances?accountType={accountType}";
        return await SendRequestAsync<List<BlofinBalance>>(credentials, endpoint, ct);
    }

    /// <summary>
    /// Gets total balance snapshot across futures and funding accounts.
    /// Uses /api/v1/account/balance for futures (returns totalEquity in USD)
    /// and /api/v1/asset/balances for funding account.
    /// </summary>
    public async Task<Result<BalanceSnapshot>> GetBalanceSnapshotAsync(
        BlofinCredentials credentials,
        CancellationToken ct = default)
    {
        var balancesByAccount = new Dictionary<string, decimal>();
        var totalUsd = 0m;

        // 1. Get futures account balance (this gives us totalEquity in USD for all currencies)
        var futuresResult = await GetFuturesAccountBalanceAsync(credentials, ct);
        if (futuresResult.IsSuccess)
        {
            var futuresEquity = futuresResult.Value.TotalEquityAsDecimal;
            logger.LogInformation(
                "Futures account balance - TotalEquity: {TotalEquity}, TotalEquityDecimal: {Decimal}, Details count: {Count}",
                futuresResult.Value.TotalEquity,
                futuresEquity,
                futuresResult.Value.Details?.Count ?? 0);

            if (futuresEquity > 0)
            {
                balancesByAccount[BlofinAccountTypes.Futures] = futuresEquity;
                totalUsd += futuresEquity;
            }
        }
        else
        {
            logger.LogWarning(
                "Failed to get futures balance: {Error}",
                string.Join(", ", futuresResult.Errors.Select(e => e.Message)));
        }

        // 2. Get funding account balance (USDT only for now, as this endpoint doesn't provide USD conversion)
        await Task.Delay(50, ct); // Rate limit
        var fundingResult = await GetAccountBalancesAsync(credentials, BlofinAccountTypes.Funding, ct);
        if (fundingResult.IsSuccess)
        {
            var fundingUsdt = fundingResult.Value
                .Where(b => b.Currency.Equals("USDT", StringComparison.OrdinalIgnoreCase))
                .Sum(b => b.BalanceAsDecimal);

            if (fundingUsdt > 0)
            {
                balancesByAccount[BlofinAccountTypes.Funding] = fundingUsdt;
                totalUsd += fundingUsdt; // USDT ≈ USD
            }
        }
        else
        {
            logger.LogWarning(
                "Failed to get funding balance: {Error}",
                string.Join(", ", fundingResult.Errors.Select(e => e.Message)));
        }

        logger.LogInformation(
            "Balance snapshot complete - TotalUsd: {TotalUsd}, Accounts: {Accounts}",
            totalUsd,
            string.Join(", ", balancesByAccount.Select(kv => $"{kv.Key}={kv.Value}")));

        return Result.Ok(new BalanceSnapshot(
            TotalUsd: totalUsd,
            BalancesByAccount: balancesByAccount,
            SnapshotTime: DateTime.UtcNow));
    }

    private async Task<Result<T>> SendRequestAsync<T>(
        BlofinCredentials credentials,
        string endpoint,
        CancellationToken ct)
    {
        try
        {
            var client = httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(BlofinSignatureService.GetBaseUrl(credentials.IsDemo));
            client.Timeout = TimeSpan.FromSeconds(30);

            var headers = BlofinSignatureService.CreateAuthHeaders(
                credentials,
                endpoint,
                "GET");

            var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            foreach (var header in headers)
            {
                request.Headers.Add(header.Key, header.Value);
            }

            var response = await client.SendAsync(request, ct);
            var content = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Blofin API request failed: {StatusCode} - {Content}",
                    response.StatusCode,
                    content);
                return Result.Fail($"API request failed: {response.StatusCode}");
            }

            logger.LogDebug("Blofin API response: {Content}", content);

            var apiResponse = JsonSerializer.Deserialize<BlofinApiResponse<T>>(content, JsonOptions);
            if (apiResponse is null)
            {
                logger.LogWarning("Failed to deserialize Blofin API response: {Content}", content);
                return Result.Fail("Failed to deserialize API response");
            }

            if (!apiResponse.IsSuccess)
            {
                logger.LogWarning(
                    "Blofin API error: {Code} - {Message}",
                    apiResponse.Code,
                    apiResponse.Message);
                return Result.Fail($"API error: {apiResponse.Message}");
            }

            logger.LogDebug("Blofin API data type: {Type}, IsNull: {IsNull}",
                typeof(T).Name,
                apiResponse.Data is null);

            return Result.Ok(apiResponse.Data!);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error calling Blofin API: {Endpoint}", endpoint);
            return Result.Fail($"Exception: {ex.Message}");
        }
    }
}

/// <summary>
/// Snapshot of account balances at a point in time.
/// TotalUsd includes all currencies converted to USD equivalent.
/// </summary>
internal sealed record BalanceSnapshot(
    decimal TotalUsd,
    Dictionary<string, decimal> BalancesByAccount,
    DateTime SnapshotTime);
