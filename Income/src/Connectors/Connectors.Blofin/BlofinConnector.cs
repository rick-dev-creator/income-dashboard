using System.Text.Json;
using Connectors.Blofin.Models;
using Connectors.Blofin.Services;
using FluentResults;
using Income.Application.Connectors;

namespace Connectors.Blofin;

/// <summary>
/// Blofin exchange connector for syncing wallet balance snapshots.
/// Captures the current total balance in USD equivalent across all account types and currencies.
/// </summary>
internal sealed class BlofinConnector(BlofinApiClient apiClient) : ISyncableConnector
{
    public const string Id = "blofin";

    public string ProviderId => Id;

    public string DisplayName => "Blofin Exchange";

    public string ProviderType => "Exchange";

    public ConnectorKind Kind => ConnectorKind.Syncable;

    public string DefaultCurrency => "USD";

    public string ConfigSchema => """
        {
          "type": "object",
          "properties": {
            "apiKey": {
              "type": "string",
              "title": "API Key",
              "description": "Your Blofin API Key"
            },
            "secretKey": {
              "type": "string",
              "title": "Secret Key",
              "description": "Your Blofin Secret Key"
            },
            "passphrase": {
              "type": "string",
              "title": "Passphrase",
              "description": "The passphrase you set when creating the API Key"
            },
            "isDemo": {
              "type": "boolean",
              "title": "Demo Mode",
              "description": "Use demo trading environment",
              "default": false
            }
          },
          "required": ["apiKey", "secretKey", "passphrase"]
        }
        """;

    public TimeSpan SyncInterval => TimeSpan.FromMinutes(5); // Testing interval

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
    /// Fetches a balance snapshot for today.
    /// Since we can only get current balance (not historical), this returns
    /// a single snapshot for today's date regardless of the requested range.
    /// Historical data is built up over time through daily syncs.
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

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Only fetch if today is within the requested range
        if (today < from || today > to)
        {
            return Result.Ok<IReadOnlyList<SyncedSnapshotData>>([]);
        }

        var result = await apiClient.GetBalanceSnapshotAsync(credentials, ct);
        if (result.IsFailed)
            return Result.Fail(result.Errors);

        var snapshot = result.Value;

        // Return a single snapshot for today with total USD balance (all currencies converted)
        var snapshots = new List<SyncedSnapshotData>();

        if (snapshot.TotalUsd > 0)
        {
            snapshots.Add(new SyncedSnapshotData(
                Date: today,
                OriginalAmount: snapshot.TotalUsd,
                OriginalCurrency: "USD",
                UsdAmount: snapshot.TotalUsd,
                ExchangeRate: 1.0m,
                RateSource: "Blofin"));
        }

        return Result.Ok<IReadOnlyList<SyncedSnapshotData>>(snapshots);
    }

    private static BlofinCredentials? ParseCredentials(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<BlofinCredentials>(json, new JsonSerializerOptions
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
