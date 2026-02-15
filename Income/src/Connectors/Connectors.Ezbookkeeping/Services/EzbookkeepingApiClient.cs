using System.Text.Json;
using Connectors.Ezbookkeeping.Models;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace Connectors.Ezbookkeeping.Services;

/// <summary>
/// HTTP client for ezbookkeeping API calls.
/// </summary>
internal sealed class EzbookkeepingApiClient(
    IHttpClientFactory httpClientFactory,
    ILogger<EzbookkeepingApiClient> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Validates credentials by fetching the user profile.
    /// </summary>
    public async Task<Result> ValidateCredentialsAsync(
        EzbookkeepingCredentials credentials,
        CancellationToken ct = default)
    {
        var result = await GetUserProfileAsync(credentials, ct);
        return result.IsSuccess
            ? Result.Ok()
            : Result.Fail(result.Errors);
    }

    /// <summary>
    /// Gets the current user profile.
    /// </summary>
    public async Task<Result<EzbookkeepingUserProfile>> GetUserProfileAsync(
        EzbookkeepingCredentials credentials,
        CancellationToken ct = default)
    {
        return await SendRequestAsync<EzbookkeepingUserProfile>(
            credentials, "/api/v1/users/profile/get.json", ct);
    }

    /// <summary>
    /// Gets the latest exchange rates from ezbookkeeping.
    /// Rates are relative to a base currency (typically EUR from ECB).
    /// </summary>
    public async Task<Result<EzbookkeepingExchangeRates>> GetExchangeRatesAsync(
        EzbookkeepingCredentials credentials,
        CancellationToken ct = default)
    {
        return await SendRequestAsync<EzbookkeepingExchangeRates>(
            credentials, "/api/v1/exchange_rates/latest.json", ct);
    }

    /// <summary>
    /// Gets all transactions for a date range.
    /// Uses the /transactions/list/all.json endpoint which returns all transactions without pagination.
    /// </summary>
    public async Task<Result<List<EzbookkeepingTransaction>>> GetTransactionsAsync(
        EzbookkeepingCredentials credentials,
        DateOnly from,
        DateOnly to,
        CancellationToken ct = default)
    {
        var startTime = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero)
            .ToUnixTimeSeconds();
        var endTime = new DateTimeOffset(to.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero)
            .ToUnixTimeSeconds();

        var endpoint = $"/api/v1/transactions/list/all.json?start_time={startTime}&end_time={endTime}";

        return await SendRequestAsync<List<EzbookkeepingTransaction>>(credentials, endpoint, ct);
    }

    private async Task<Result<T>> SendRequestAsync<T>(
        EzbookkeepingCredentials credentials,
        string endpoint,
        CancellationToken ct)
    {
        try
        {
            var client = httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(credentials.GetBaseUrl());
            client.Timeout = TimeSpan.FromSeconds(30);

            var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            request.Headers.Add("Authorization", $"Bearer {credentials.ApiToken}");
            request.Headers.Add("X-Timezone-Name", "UTC");
            request.Headers.Add("X-Timezone-Offset", "0");

            var response = await client.SendAsync(request, ct);
            var content = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "ezbookkeeping API request failed: {StatusCode} - {Content}",
                    response.StatusCode,
                    content);
                return Result.Fail($"API request failed: {response.StatusCode}");
            }

            logger.LogDebug("ezbookkeeping API response: {Content}", content);

            var apiResponse = JsonSerializer.Deserialize<EzbookkeepingApiResponse<T>>(content, JsonOptions);
            if (apiResponse is null)
            {
                logger.LogWarning("Failed to deserialize ezbookkeeping API response: {Content}", content);
                return Result.Fail("Failed to deserialize API response");
            }

            if (!apiResponse.Success)
            {
                logger.LogWarning(
                    "ezbookkeeping API error: {Code} - {Message}",
                    apiResponse.ErrorCode,
                    apiResponse.ErrorMessage);
                return Result.Fail($"API error: {apiResponse.ErrorMessage}");
            }

            return Result.Ok(apiResponse.Result!);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error calling ezbookkeeping API: {Endpoint}", endpoint);
            return Result.Fail($"Exception: {ex.Message}");
        }
    }
}
