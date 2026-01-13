using Income.Application.Services;
using Income.Application.Services.Streams;
using Income.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Income.Infrastructure.Jobs;

/// <summary>
/// TEST ONLY: Background job that generates random snapshot variations every 5 minutes.
/// This allows seeing real-time changes in the dashboard during testing.
/// Disable or remove this job in production.
/// </summary>
internal sealed class TestDataGeneratorJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IActivityLogService _activityLog;
    private readonly ILogger<TestDataGeneratorJob> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);
    private readonly Random _random = new();

    public TestDataGeneratorJob(
        IServiceScopeFactory scopeFactory,
        IActivityLogService activityLog,
        ILogger<TestDataGeneratorJob> logger)
    {
        _scopeFactory = scopeFactory;
        _activityLog = activityLog;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TestDataGeneratorJob started. Generating test data every {Interval} minutes", _interval.TotalMinutes);
        _activityLog.LogInfo("system", "TestDataJob", $"Test data generator started. Updating snapshots every {_interval.TotalMinutes} minutes.");

        // Wait a bit before first run to let other services initialize
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await GenerateTestDataAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error generating test data");
                _activityLog.LogError("system", "TestDataJob", "Error generating test data", ex.Message);
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task GenerateTestDataAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<IncomeDbContext>>();
        var streamService = scope.ServiceProvider.GetRequiredService<IStreamService>();

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        // Get recurring streams for test data generation
        var recurringStreams = await dbContext.Streams
            .Where(s => s.RecurringAmount != null)
            .Where(s => s.SyncState != (int)Domain.StreamContext.ValueObjects.SyncState.Disabled)
            .ToListAsync(ct);

        if (recurringStreams.Count == 0)
        {
            _activityLog.LogInfo("system", "TestDataJob", "No recurring streams to update.");
            return;
        }

        _activityLog.LogInfo("system", "TestDataJob", $"Updating {recurringStreams.Count} recurring stream(s) with test data...");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        foreach (var stream in recurringStreams)
        {
            try
            {
                // Generate random variation (-10% to +10%)
                var baseAmount = stream.RecurringAmount!.Value;
                var variation = 1 + (decimal)(_random.NextDouble() * 0.2 - 0.1);
                var newAmount = Math.Round(baseAmount * variation, 2);

                // Record/update snapshot for today
                var result = await streamService.RecordSnapshotAsync(new RecordSnapshotRequest(
                    StreamId: stream.Id,
                    Date: today,
                    OriginalAmount: newAmount,
                    OriginalCurrency: stream.OriginalCurrency,
                    UsdAmount: newAmount, // Assuming USD for test
                    ExchangeRate: 1.0m,
                    RateSource: "TestData"), ct);

                if (result.IsSuccess)
                {
                    _activityLog.LogSuccess(
                        stream.Id,
                        stream.Name,
                        $"Test data updated: ${newAmount:N2} (base: ${baseAmount:N2})",
                        newAmount);

                    _logger.LogDebug("Updated test data for stream {StreamId}: {Amount}", stream.Id, newAmount);
                }
                else
                {
                    _activityLog.LogError(stream.Id, stream.Name, "Failed to update test data",
                        string.Join(", ", result.Errors.Select(e => e.Message)));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating test data for stream {StreamId}", stream.Id);
                _activityLog.LogError(stream.Id, stream.Name, "Error updating test data", ex.Message);
            }
        }

        _activityLog.LogInfo("system", "TestDataJob", "Test data generation cycle completed.");
    }
}
