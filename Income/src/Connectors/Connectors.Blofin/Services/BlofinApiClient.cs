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
    /// Validates credentials by fetching account balances.
    /// </summary>
    public async Task<Result> ValidateCredentialsAsync(
        BlofinCredentials credentials,
        CancellationToken ct = default)
    {
        // Try to fetch funding balance as a validation check
        var result = await GetAccountBalancesAsync(
            credentials,
            BlofinAccountTypes.Funding,
            ct);

        return result.IsSuccess
            ? Result.Ok()
            : Result.Fail(result.Errors);
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
    /// Gets total balance across all account types in USDT.
    /// </summary>
    public async Task<Result<decimal>> GetTotalBalanceUsdtAsync(
        BlofinCredentials credentials,
        CancellationToken ct = default)
    {
        var totalUsdt = 0m;

        foreach (var accountType in BlofinAccountTypes.All)
        {
            var result = await GetAccountBalancesAsync(credentials, accountType, ct);

            if (result.IsFailed)
            {
                logger.LogWarning(
                    "Failed to get balances for {AccountType}: {Error}",
                    accountType,
                    string.Join(", ", result.Errors.Select(e => e.Message)));
                continue; // Skip failed account types, don't fail entire operation
            }

            foreach (var balance in result.Value)
            {
                // Sum USDT balances directly
                if (balance.Currency.Equals("USDT", StringComparison.OrdinalIgnoreCase))
                {
                    totalUsdt += balance.BalanceAsDecimal;
                }
                // For other currencies, we'd need conversion rates
                // For now, we'll only track USDT to keep it simple
            }

            // Small delay between requests to respect rate limits
            await Task.Delay(50, ct);
        }

        return Result.Ok(totalUsdt);
    }

    /// <summary>
    /// Gets detailed balance breakdown across all accounts.
    /// </summary>
    public async Task<Result<BalanceSnapshot>> GetBalanceSnapshotAsync(
        BlofinCredentials credentials,
        CancellationToken ct = default)
    {
        var balancesByAccount = new Dictionary<string, decimal>();
        var totalUsdt = 0m;

        foreach (var accountType in BlofinAccountTypes.All)
        {
            var result = await GetAccountBalancesAsync(credentials, accountType, ct);

            if (result.IsFailed)
            {
                logger.LogWarning(
                    "Failed to get balances for {AccountType}",
                    accountType);
                continue;
            }

            var accountUsdt = result.Value
                .Where(b => b.Currency.Equals("USDT", StringComparison.OrdinalIgnoreCase))
                .Sum(b => b.BalanceAsDecimal);

            if (accountUsdt > 0)
            {
                balancesByAccount[accountType] = accountUsdt;
                totalUsdt += accountUsdt;
            }

            await Task.Delay(50, ct);
        }

        return Result.Ok(new BalanceSnapshot(
            TotalUsdt: totalUsdt,
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

            var apiResponse = JsonSerializer.Deserialize<BlofinApiResponse<T>>(content, JsonOptions);
            if (apiResponse is null)
            {
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
/// </summary>
internal sealed record BalanceSnapshot(
    decimal TotalUsdt,
    Dictionary<string, decimal> BalancesByAccount,
    DateTime SnapshotTime);
