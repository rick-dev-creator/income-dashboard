using Income.Application.Connectors;
using Income.Application.Services.Streams;
using Income.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Income.Infrastructure.Jobs;

/// <summary>
/// Background job that generates snapshots for recurring income streams.
/// Checks daily for payments that are due based on schedule (salary, rent, etc.).
/// </summary>
internal sealed class RecurringJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RecurringJob> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1);

    public RecurringJob(
        IServiceScopeFactory scopeFactory,
        ILogger<RecurringJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RecurringJob started. Checking for due payments every {Interval} hour(s)",
            _checkInterval.TotalHours);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessRecurringStreamsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error processing recurring streams");
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
            return;
        }

        _logger.LogDebug("Found {Count} recurring streams to check", recurringStreams.Count);

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
                    continue;
                }

                // Check if we already have a snapshot for today
                var existingSnapshot = streamEntity.Snapshots.Any(s => s.Date == today);
                if (existingSnapshot)
                {
                    _logger.LogDebug("Snapshot already exists for today for stream {StreamId}", streamEntity.Id);
                    continue;
                }

                _logger.LogInformation("Generating recurring snapshot for stream {StreamId} ({StreamName})",
                    streamEntity.Id, streamEntity.Name);

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
                    _logger.LogWarning("Failed to record recurring snapshot for stream {StreamId}: {Errors}",
                        streamEntity.Id, string.Join(", ", result.Errors));
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
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error processing recurring stream {StreamId}", streamEntity.Id);
            }
        }
    }
}
