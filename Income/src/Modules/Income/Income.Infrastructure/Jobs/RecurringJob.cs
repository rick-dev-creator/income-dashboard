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
/// Background job that generates snapshots for recurring income streams.
/// Checks for payments that are due based on schedule (salary, rent, etc.).
/// </summary>
internal sealed class RecurringJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IActivityLogService _activityLog;
    private readonly ILogger<RecurringJob> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5); // Testing interval

    public RecurringJob(
        IServiceScopeFactory scopeFactory,
        IActivityLogService activityLog,
        ILogger<RecurringJob> logger)
    {
        _scopeFactory = scopeFactory;
        _activityLog = activityLog;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RecurringJob started. Checking for due payments every {Interval} minutes",
            _checkInterval.TotalMinutes);

        _activityLog.LogInfo("system", "RecurringJob", $"Background recurring job started. Checking every {_checkInterval.TotalMinutes} minutes.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessRecurringStreamsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error processing recurring streams");
                _activityLog.LogError("system", "RecurringJob", "Error processing recurring streams", ex.Message);
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task ProcessRecurringStreamsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<IncomeDbContext>>();
        var registry = scope.ServiceProvider.GetRequiredService<IConnectorRegistry>();
        var streamService = scope.ServiceProvider.GetRequiredService<IStreamService>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        // Get recurring streams
        var recurringStreams = await dbContext.Streams
            .Include(s => s.Snapshots)
            .Where(s => s.RecurringAmount != null)
            .Where(s => s.RecurringFrequency != null)
            .Where(s => s.RecurringStartDate != null)
            .Where(s => s.SyncState != (int)Domain.StreamContext.ValueObjects.SyncState.Disabled)
            .ToListAsync(ct);

        if (recurringStreams.Count == 0)
        {
            _logger.LogDebug("No recurring streams configured");
            _activityLog.LogInfo("system", "RecurringJob", "Recurring check completed. No recurring streams configured.");
            return;
        }

        _logger.LogDebug("Found {Count} recurring streams to check", recurringStreams.Count);
        _activityLog.LogInfo("system", "RecurringJob", $"Found {recurringStreams.Count} recurring stream(s) to check.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        foreach (var streamEntity in recurringStreams)
        {
            try
            {
                var connector = registry.GetRecurringById(streamEntity.ProviderId);
                if (connector is null)
                {
                    // Use the default fixed income connector
                    connector = registry.GetRecurring().FirstOrDefault();
                    if (connector is null)
                    {
                        _logger.LogWarning("No recurring connector available for stream {StreamId}", streamEntity.Id);
                        _activityLog.LogWarning(streamEntity.Id, streamEntity.Name, "No recurring connector available");
                        continue;
                    }
                }

                var frequency = (RecurringFrequency)streamEntity.RecurringFrequency!.Value;
                var startDate = streamEntity.RecurringStartDate!.Value;
                var amount = streamEntity.RecurringAmount!.Value;

                // Check if payment is due today and not already recorded
                if (!connector.IsPaymentDue(startDate, frequency, today))
                {
                    _logger.LogDebug("No payment due today for stream {StreamId}", streamEntity.Id);
                    _activityLog.LogInfo(streamEntity.Id, streamEntity.Name, $"No payment due today ({frequency})");
                    continue;
                }

                // Check if we already have a snapshot for today
                var existingSnapshot = streamEntity.Snapshots.Any(s => s.Date == today);
                if (existingSnapshot)
                {
                    _logger.LogDebug("Snapshot already exists for today for stream {StreamId}", streamEntity.Id);
                    _activityLog.LogInfo(streamEntity.Id, streamEntity.Name, "Snapshot already exists for today");
                    continue;
                }

                _logger.LogInformation("Generating recurring snapshot for stream {StreamId} ({StreamName})",
                    streamEntity.Id, streamEntity.Name);
                _activityLog.LogInfo(streamEntity.Id, streamEntity.Name, $"Generating recurring snapshot ({frequency})...");

                // Generate snapshot
                var snapshotData = connector.GenerateSnapshot(amount, streamEntity.OriginalCurrency, today);

                // Record the snapshot
                var result = await streamService.RecordSnapshotAsync(new RecordSnapshotRequest(
                    StreamId: streamEntity.Id,
                    Date: snapshotData.Date,
                    OriginalAmount: snapshotData.OriginalAmount,
                    OriginalCurrency: snapshotData.OriginalCurrency,
                    UsdAmount: snapshotData.UsdAmount,
                    ExchangeRate: snapshotData.ExchangeRate,
                    RateSource: snapshotData.RateSource), ct);

                if (result.IsFailed)
                {
                    var errorMsg = string.Join(", ", result.Errors.Select(e => e.Message));
                    _logger.LogWarning("Failed to record recurring snapshot for stream {StreamId}: {Errors}",
                        streamEntity.Id, errorMsg);
                    _activityLog.LogError(streamEntity.Id, streamEntity.Name, "Failed to record snapshot", errorMsg);

                    // Create error notification (contextual based on stream type)
                    var isExpense = streamEntity.StreamType == 1;
                    var failedTitle = isExpense
                        ? $"Recurring expense failed: {streamEntity.Name}"
                        : $"Recurring payment failed: {streamEntity.Name}";

                    await notificationService.CreateAsync(new CreateNotificationRequest(
                        Type: NotificationTypes.RecurringError,
                        Title: failedTitle,
                        Message: errorMsg,
                        StreamId: streamEntity.Id,
                        StreamName: streamEntity.Name), ct);
                    continue;
                }

                // Update sync status
                streamEntity.LastSuccessAt = DateTime.UtcNow;
                streamEntity.LastAttemptAt = DateTime.UtcNow;
                var nextPaymentDate = connector.CalculateNextPaymentDate(startDate, frequency, today.AddDays(1));
                streamEntity.NextScheduledAt = DateTime.SpecifyKind(
                    nextPaymentDate.ToDateTime(TimeOnly.MinValue),
                    DateTimeKind.Utc);
                await dbContext.SaveChangesAsync(ct);

                _logger.LogInformation("Successfully generated recurring snapshot for stream {StreamId}. Amount: {Amount} {Currency}",
                    streamEntity.Id, amount, streamEntity.OriginalCurrency);
                _activityLog.LogSuccess(
                    streamEntity.Id,
                    streamEntity.Name,
                    $"Recurring snapshot recorded. Next: {nextPaymentDate:yyyy-MM-dd}",
                    amount);

                // Create success notification (contextual based on stream type)
                var isOutcome = streamEntity.StreamType == 1;
                var successTitle = isOutcome
                    ? $"Recurring expense processed: {streamEntity.Name}"
                    : $"Recurring payment received: {streamEntity.Name}";
                var successMessage = isOutcome
                    ? $"Expense of ${amount:N2} {streamEntity.OriginalCurrency} recorded. Next expense: {nextPaymentDate:yyyy-MM-dd}"
                    : $"Payment of ${amount:N2} {streamEntity.OriginalCurrency} recorded. Next payment: {nextPaymentDate:yyyy-MM-dd}";

                await notificationService.CreateAsync(new CreateNotificationRequest(
                    Type: NotificationTypes.RecurringSuccess,
                    Title: successTitle,
                    Message: successMessage,
                    StreamId: streamEntity.Id,
                    StreamName: streamEntity.Name), ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error processing recurring stream {StreamId}", streamEntity.Id);
                _activityLog.LogError(streamEntity.Id, streamEntity.Name, "Error processing recurring stream", ex.Message);

                // Create error notification (contextual based on stream type)
                var isExpenseError = streamEntity.StreamType == 1;
                var errorTitle = isExpenseError
                    ? $"Recurring expense error: {streamEntity.Name}"
                    : $"Recurring payment error: {streamEntity.Name}";

                await notificationService.CreateAsync(new CreateNotificationRequest(
                    Type: NotificationTypes.RecurringError,
                    Title: errorTitle,
                    Message: ex.Message,
                    StreamId: streamEntity.Id,
                    StreamName: streamEntity.Name), ct);
            }
        }
    }
}
