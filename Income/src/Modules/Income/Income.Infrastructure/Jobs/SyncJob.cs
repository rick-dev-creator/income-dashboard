using Income.Application.Connectors;
using Income.Application.Services;
using Income.Application.Services.Notifications;
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
    private readonly IActivityLogService _activityLog;
    private readonly ILogger<SyncJob> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5);

    public SyncJob(
        IServiceScopeFactory scopeFactory,
        IActivityLogService activityLog,
        ILogger<SyncJob> logger)
    {
        _scopeFactory = scopeFactory;
        _activityLog = activityLog;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SyncJob started. Checking for streams to sync every {Interval} minutes",
            _checkInterval.TotalMinutes);

        _activityLog.LogInfo("system", "SyncJob", $"Background sync job started. Checking every {_checkInterval.TotalMinutes} minutes.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessSyncableStreamsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error processing syncable streams");
                _activityLog.LogError("system", "SyncJob", "Error processing syncable streams", ex.Message);
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
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

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
            _activityLog.LogInfo("system", "SyncJob", "Sync check completed. No streams due for sync.");
            return;
        }

        _logger.LogInformation("Found {Count} streams due for sync", streamsToSync.Count);
        _activityLog.LogInfo("system", "SyncJob", $"Found {streamsToSync.Count} stream(s) due for sync.");

        foreach (var streamEntity in streamsToSync)
        {
            try
            {
                var connector = registry.GetSyncableById(streamEntity.ProviderId);
                if (connector is null)
                {
                    _logger.LogWarning("No connector found for provider {ProviderId}", streamEntity.ProviderId);
                    _activityLog.LogWarning(streamEntity.Id, streamEntity.Name, $"No connector found for provider '{streamEntity.ProviderId}'");
                    continue;
                }

                _activityLog.LogInfo(streamEntity.Id, streamEntity.Name, $"Starting sync via {connector.DisplayName}...");

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
                    var errorMsg = string.Join(", ", result.Errors.Select(e => e.Message));
                    _logger.LogWarning("Failed to fetch snapshots for stream {StreamId}: {Errors}",
                        streamEntity.Id, errorMsg);

                    _activityLog.LogError(streamEntity.Id, streamEntity.Name, "Sync failed", errorMsg);

                    // Create error notification
                    await notificationService.CreateAsync(new CreateNotificationRequest(
                        Type: NotificationTypes.SyncError,
                        Title: $"Sync failed for {streamEntity.Name}",
                        Message: errorMsg,
                        StreamId: streamEntity.Id,
                        StreamName: streamEntity.Name), ct);

                    // Mark as failed
                    streamEntity.SyncState = (int)Domain.StreamContext.ValueObjects.SyncState.Failed;
                    streamEntity.LastAttemptAt = DateTime.UtcNow;
                    streamEntity.LastError = errorMsg;
                    await dbContext.SaveChangesAsync(ct);
                    continue;
                }

                // Record each snapshot
                decimal totalAmount = 0;
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
                    totalAmount = snapshotData.UsdAmount;
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

                _activityLog.LogSuccess(
                    streamEntity.Id,
                    streamEntity.Name,
                    $"Sync completed. Recorded {result.Value.Count} snapshot(s). Next sync: {streamEntity.NextScheduledAt:HH:mm:ss}",
                    totalAmount);

                // Create success notification
                await notificationService.CreateAsync(new CreateNotificationRequest(
                    Type: NotificationTypes.SyncSuccess,
                    Title: $"Sync completed for {streamEntity.Name}",
                    Message: $"Successfully synced {result.Value.Count} snapshot(s). Current balance: ${totalAmount:N2}",
                    StreamId: streamEntity.Id,
                    StreamName: streamEntity.Name), ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error syncing stream {StreamId}", streamEntity.Id);
                _activityLog.LogError(streamEntity.Id, streamEntity.Name, "Sync error", ex.Message);

                // Create error notification
                await notificationService.CreateAsync(new CreateNotificationRequest(
                    Type: NotificationTypes.SyncError,
                    Title: $"Sync error for {streamEntity.Name}",
                    Message: ex.Message,
                    StreamId: streamEntity.Id,
                    StreamName: streamEntity.Name), ct);

                streamEntity.SyncState = (int)Domain.StreamContext.ValueObjects.SyncState.Failed;
                streamEntity.LastAttemptAt = DateTime.UtcNow;
                streamEntity.LastError = ex.Message;
                await dbContext.SaveChangesAsync(ct);
            }
        }
    }
}
