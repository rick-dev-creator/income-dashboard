using System.Text.Json;
using Connectors.Ezbookkeeping.Models;
using Connectors.Ezbookkeeping.Services;
using FluentResults;
using Income.Application.Connectors;

namespace Connectors.Ezbookkeeping;

/// <summary>
/// ezbookkeeping connector for syncing income and expense flow snapshots.
/// Aggregates all transactions by day into a single daily total per stream.
/// This is NOT granular budgeting - it captures the big-picture flow:
/// total income per day and total expenses per day.
///
/// Handles multi-currency transactions by converting everything to USD
/// using ezbookkeeping's exchange rate API (ECB rates).
///
/// Each stream stores a "streamMode" in its credentials ("income" or "expense")
/// so the connector knows which transaction type to aggregate.
/// </summary>
internal sealed class EzbookkeepingConnector(EzbookkeepingApiClient apiClient) : ISyncableConnector
{
    public const string Id = "ezbookkeeping";

    public string ProviderId => Id;

    public string DisplayName => "ezbookkeeping";

    public string ProviderType => "Budgeting";

    public ConnectorKind Kind => ConnectorKind.Syncable;

    public string DefaultCurrency => "USD";

    /// <summary>
    /// Supports both Income and Outcome streams.
    /// Income stream: aggregates all income transactions per day.
    /// Outcome stream: aggregates all expense transactions per day.
    /// </summary>
    public SupportedStreamTypes SupportedStreamTypes => SupportedStreamTypes.Both;

    public string ConfigSchema => """
        {
          "type": "object",
          "properties": {
            "serverUrl": {
              "type": "string",
              "title": "Server URL",
              "description": "Your ezbookkeeping server URL (e.g., https://ezbook.example.com)"
            },
            "apiToken": {
              "type": "string",
              "title": "API Token",
              "description": "Long-lived API token generated from ezbookkeeping"
            }
          },
          "required": ["serverUrl", "apiToken"]
        }
        """;

    public TimeSpan SyncInterval => TimeSpan.FromHours(1);

    public async Task<Result> ValidateCredentialsAsync(
        string decryptedCredentials,
        CancellationToken ct = default)
    {
        var credentials = ParseCredentials(decryptedCredentials);
        if (credentials is null)
            return Result.Fail("Invalid credentials format");

        if (!credentials.IsValid())
            return Result.Fail("Missing required credential fields");

        return await apiClient.ValidateCredentialsAsync(credentials, ct);
    }

    /// <summary>
    /// Fetches transactions for the given date range and aggregates by day.
    /// Converts all amounts to USD using ezbookkeeping's exchange rates.
    /// Filters by transaction type based on the streamMode in credentials:
    /// - "income" mode → only type=2 (Income) transactions
    /// - "expense" mode → only type=3 (Expense) transactions
    /// </summary>
    public async Task<Result<IReadOnlyList<SyncedSnapshotData>>> FetchSnapshotsAsync(
        string decryptedCredentials,
        DateOnly from,
        DateOnly to,
        CancellationToken ct = default)
    {
        var credentials = ParseCredentials(decryptedCredentials);
        if (credentials is null)
            return Result.Fail("Invalid credentials format");

        // Fetch transactions and exchange rates in parallel
        var transactionsTask = apiClient.GetTransactionsAsync(credentials, from, to, ct);
        var ratesTask = apiClient.GetExchangeRatesAsync(credentials, ct);

        var transactionsResult = await transactionsTask;
        if (transactionsResult.IsFailed)
            return Result.Fail(transactionsResult.Errors);

        // Build currency converter from exchange rates
        var converter = await BuildCurrencyConverterAsync(ratesTask);

        var transactions = transactionsResult.Value;

        var targetType = credentials.IsIncomeMode
            ? EzbookkeepingTransactionTypes.Income
            : EzbookkeepingTransactionTypes.Expense;

        var rateSource = credentials.IsIncomeMode
            ? "ezbookkeeping-income"
            : "ezbookkeeping-expense";

        var snapshots = transactions
            .Where(t => t.Type == targetType)
            .GroupBy(t => t.Date)
            .Select(g =>
            {
                // Sum each transaction converted to USD individually
                var totalUsd = 0m;
                var totalOriginal = 0m;
                string? predominantCurrency = null;

                foreach (var t in g)
                {
                    var currency = t.SourceAccount?.Currency ?? "USD";
                    var amount = t.AmountAsDecimal;
                    var usd = converter(amount, currency);

                    totalUsd += usd;
                    totalOriginal += amount;

                    // Track the currency with the most transactions for OriginalCurrency
                    predominantCurrency ??= currency;
                }

                var exchangeRate = totalOriginal != 0
                    ? totalUsd / totalOriginal
                    : 1.0m;

                return new SyncedSnapshotData(
                    Date: g.Key,
                    OriginalAmount: totalOriginal,
                    OriginalCurrency: predominantCurrency ?? "USD",
                    UsdAmount: totalUsd,
                    ExchangeRate: Math.Round(exchangeRate, 6),
                    RateSource: rateSource);
            })
            .ToList();

        return Result.Ok<IReadOnlyList<SyncedSnapshotData>>(snapshots);
    }

    /// <summary>
    /// Builds a function that converts any currency amount to USD.
    /// ezbookkeeping rates are relative to a base currency (typically EUR from ECB).
    /// To convert X JPY to USD: (X / JPY_rate) * USD_rate
    /// </summary>
    private static async Task<Func<decimal, string, decimal>> BuildCurrencyConverterAsync(
        Task<Result<EzbookkeepingExchangeRates>> ratesTask)
    {
        var ratesResult = await ratesTask;

        if (ratesResult.IsFailed)
        {
            // Fallback: no conversion, assume USD
            return (amount, _) => amount;
        }

        var rates = ratesResult.Value;
        var rateMap = rates.ExchangeRates
            .ToDictionary(r => r.Currency.ToUpperInvariant(), r => r.RateAsDecimal);

        // Add base currency with rate 1.0
        var baseCurrency = rates.BaseCurrency.ToUpperInvariant();
        rateMap[baseCurrency] = 1.0m;

        return (amount, currency) =>
        {
            var curr = currency.ToUpperInvariant();

            if (curr == "USD")
                return amount;

            // Get rate for source currency (relative to base)
            if (!rateMap.TryGetValue(curr, out var sourceRate) || sourceRate == 0)
                return amount; // Unknown currency, return as-is

            // Get rate for USD (relative to base)
            if (!rateMap.TryGetValue("USD", out var usdRate) || usdRate == 0)
                return amount; // No USD rate, return as-is

            // Convert: source → base → USD
            return (amount / sourceRate) * usdRate;
        };
    }

    private static EzbookkeepingCredentials? ParseCredentials(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<EzbookkeepingCredentials>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            return null;
        }
    }
}
