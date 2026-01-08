using FluentResults;

namespace Income.Application.Connectors;

/// <summary>
/// Connector that fetches income data from external APIs.
/// Used for exchanges (Binance, Coinbase), creator platforms (Patreon), etc.
/// </summary>
public interface ISyncableConnector : IIncomeConnector
{
    /// <summary>
    /// JSON Schema defining the credentials required (API keys, tokens, etc.)
    /// Used to render dynamic forms in the UI.
    /// </summary>
    string ConfigSchema { get; }

    /// <summary>
    /// How often this connector should sync (used for scheduling)
    /// </summary>
    TimeSpan SyncInterval { get; }

    /// <summary>
    /// Validates the provided credentials against the external API.
    /// </summary>
    Task<Result> ValidateCredentialsAsync(
        string decryptedCredentials,
        CancellationToken ct = default);

    /// <summary>
    /// Fetches income snapshots from the external API for the given date range.
    /// </summary>
    Task<Result<IReadOnlyList<SyncedSnapshotData>>> FetchSnapshotsAsync(
        string decryptedCredentials,
        DateOnly from,
        DateOnly to,
        CancellationToken ct = default);
}

/// <summary>
/// Data returned by a syncable connector for each income entry.
/// </summary>
public sealed record SyncedSnapshotData(
    DateOnly Date,
    decimal OriginalAmount,
    string OriginalCurrency,
    decimal UsdAmount,
    decimal ExchangeRate,
    string RateSource);
