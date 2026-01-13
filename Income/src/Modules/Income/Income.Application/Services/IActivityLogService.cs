namespace Income.Application.Services;

/// <summary>
/// Service for tracking connector activity and sync operations.
/// Provides real-time visibility into background job execution.
/// </summary>
public interface IActivityLogService
{
    /// <summary>
    /// Gets all activity entries (most recent first).
    /// </summary>
    IReadOnlyList<ActivityEntry> GetEntries(int limit = 100);

    /// <summary>
    /// Gets entries for a specific stream.
    /// </summary>
    IReadOnlyList<ActivityEntry> GetEntriesForStream(string streamId, int limit = 50);

    /// <summary>
    /// Logs an activity entry.
    /// </summary>
    void Log(ActivityEntry entry);

    /// <summary>
    /// Logs an info message.
    /// </summary>
    void LogInfo(string streamId, string streamName, string message);

    /// <summary>
    /// Logs a success message.
    /// </summary>
    void LogSuccess(string streamId, string streamName, string message, decimal? amount = null);

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    void LogWarning(string streamId, string streamName, string message);

    /// <summary>
    /// Logs an error message.
    /// </summary>
    void LogError(string streamId, string streamName, string message, string? details = null);

    /// <summary>
    /// Clears all entries.
    /// </summary>
    void Clear();

    /// <summary>
    /// Event fired when a new entry is added.
    /// </summary>
    event Action<ActivityEntry>? OnNewEntry;
}

/// <summary>
/// Represents an activity log entry.
/// </summary>
public sealed record ActivityEntry
{
    public required string Id { get; init; }
    public required DateTime Timestamp { get; init; }
    public required ActivityLevel Level { get; init; }
    public required string StreamId { get; init; }
    public required string StreamName { get; init; }
    public required string Message { get; init; }
    public string? Details { get; init; }
    public decimal? Amount { get; init; }
}

/// <summary>
/// Activity log level.
/// </summary>
public enum ActivityLevel
{
    Info,
    Success,
    Warning,
    Error
}
