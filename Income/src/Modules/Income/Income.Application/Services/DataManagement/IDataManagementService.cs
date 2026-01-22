using FluentResults;

namespace Income.Application.Services.DataManagement;

/// <summary>
/// Service for managing application data (seeding, cleanup).
/// </summary>
public interface IDataManagementService
{
    /// <summary>
    /// Creates demo streams with sample data for testing purposes.
    /// </summary>
    Task<Result<DemoDataResult>> CreateDemoDataAsync(CancellationToken ct = default);

    /// <summary>
    /// Deletes all streams and their snapshots. Providers are preserved.
    /// </summary>
    Task<Result<DeleteDataResult>> DeleteAllStreamsAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets the current data statistics.
    /// </summary>
    Task<Result<DataStatistics>> GetStatisticsAsync(CancellationToken ct = default);
}

public sealed record DemoDataResult(
    int StreamsCreated,
    int SnapshotsCreated);

public sealed record DeleteDataResult(
    int StreamsDeleted,
    int SnapshotsDeleted);

public sealed record DataStatistics(
    int TotalStreams,
    int TotalSnapshots,
    int TotalProviders);
