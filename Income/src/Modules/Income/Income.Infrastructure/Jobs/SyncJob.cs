using Income.Application.Connectors;
using Income.Application.Services;
using Income.Application.Services.Streams;
using Income.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Income.Infrastructure.Jobs;

/// <summary>
/// Background job that periodically syncs data from syncable connectors (API-based).
/// Runs on a configurable interval and fetches income data from external APIs.
/// </summary>
internal sealed class SyncJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SyncJob> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5);

    public SyncJob(
        IServiceScopeFactory scopeFactory,
        ILogger<SyncJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SyncJob started. Checking for streams to sync every {Interval} minutes",
            _checkInterval.TotalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessSyncableStreamsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error processing syncable streams");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task ProcessSyncableStreamsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<IncomeDbContext>>();
        var registry = scope.ServiceProvider.GetRequiredService<IConnectorRegistry>();
        var credentialEncryptor = scope.ServiceProvider.GetRequiredService<ICredentialEncryptor>();
        var streamService = scope.ServiceProvider.GetRequiredService<IStreamService>();

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        // Get streams that are due for sync (not recurring streams)
        var streamsToSync = await dbContext.Streams
            .Where(s => s.RecurringAmount == null) // Only syncable streams
            .Where(s => s.SyncState != (int)Domain.StreamContext.ValueObjects.SyncState.Disabled)
            .Where(s => s.NextScheduledAt == null || s.NextScheduledAt <= DateTime.UtcNow)
            .Where(s => s.EncryptedCredentials != null)
            .ToListAsync(ct);

        if (streamsToSync.Count == 0)
        {
            _logger.LogDebug("No streams due for sync");
            return;
        }

        _logger.LogInformation("Found {Count} streams due for sync", streamsToSync.Count);

        foreach (var streamEntity in streamsToSync)
        {
            try
            {
                var connector = registry.GetSyncableById(streamEntity.ProviderId);
                if (connector is null)
                {
                    _logger.LogWarning("No connector found for provider {ProviderId}", streamEntity.ProviderId);
                    continue;
                }

                // Decrypt credentials
                var decryptedCredentials = credentialEncryptor.Decrypt(streamEntity.EncryptedCredentials!);

                // Calculate date range (last 30 days by default)
                var to = DateOnly.FromDateTime(DateTime.UtcNow);
                var from = to.AddDays(-30);

                _logger.LogDebug("Syncing stream {StreamId} ({StreamName}) from {From} to {To}",
                    streamEntity.Id, streamEntity.Name, from, to);

                // Fetch snapshots from connector
                var result = await connector.FetchSnapshotsAsync(decryptedCredentials, from, to, ct);

                if (result.IsFailed)
                {
                    _logger.LogWarning("Failed to fetch snapshots for stream {StreamId}: {Errors}",
                        streamEntity.Id, string.Join(", ", result.Errors));

                    // Mark as failed
                    streamEntity.SyncState = (int)Domain.StreamContext.ValueObjects.SyncState.Failed;
                    streamEntity.LastAttemptAt = DateTime.UtcNow;
                    streamEntity.LastError = string.Join(", ", result.Errors.Select(e => e.Message));
                    await dbContext.SaveChangesAsync(ct);
                    continue;
                }

                // Record each snapshot
                foreach (var snapshotData in result.Value)
                {
                    await streamService.RecordSnapshotAsync(new RecordSnapshotRequest(
                        StreamId: streamEntity.Id,
                        Date: snapshotData.Date,
                        OriginalAmount: snapshotData.OriginalAmount,
                        OriginalCurrency: snapshotData.OriginalCurrency,
                        UsdAmount: snapshotData.UsdAmount,
                        ExchangeRate: snapshotData.ExchangeRate,
                        RateSource: snapshotData.RateSource), ct);
                }

                // Mark as success and schedule next sync
                streamEntity.SyncState = (int)Domain.StreamContext.ValueObjects.SyncState.Active;
                streamEntity.LastSuccessAt = DateTime.UtcNow;
                streamEntity.LastAttemptAt = DateTime.UtcNow;
                streamEntity.LastError = null;
                streamEntity.NextScheduledAt = DateTime.UtcNow.Add(connector.SyncInterval);
                await dbContext.SaveChangesAsync(ct);

                _logger.LogInformation("Successfully synced stream {StreamId} ({StreamName}). Recorded {Count} snapshots",
                    streamEntity.Id, streamEntity.Name, result.Value.Count);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error syncing stream {StreamId}", streamEntity.Id);

                streamEntity.SyncState = (int)Domain.StreamContext.ValueObjects.SyncState.Failed;
                streamEntity.LastAttemptAt = DateTime.UtcNow;
                streamEntity.LastError = ex.Message;
                await dbContext.SaveChangesAsync(ct);
            }
        }
    }
}
