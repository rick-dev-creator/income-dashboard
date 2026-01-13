using System.Collections.Concurrent;
using Income.Application.Services;

namespace Income.Infrastructure.Services;

/// <summary>
/// In-memory activity log service for tracking connector activity.
/// Thread-safe for use with background jobs.
/// </summary>
internal sealed class ActivityLogService : IActivityLogService
{
    private readonly ConcurrentQueue<ActivityEntry> _entries = new();
    private const int MaxEntries = 500;

    public event Action<ActivityEntry>? OnNewEntry;

    public IReadOnlyList<ActivityEntry> GetEntries(int limit = 100)
    {
        return _entries
            .OrderByDescending(e => e.Timestamp)
            .Take(limit)
            .ToList();
    }

    public IReadOnlyList<ActivityEntry> GetEntriesForStream(string streamId, int limit = 50)
    {
        return _entries
            .Where(e => e.StreamId == streamId)
            .OrderByDescending(e => e.Timestamp)
            .Take(limit)
            .ToList();
    }

    public void Log(ActivityEntry entry)
    {
        _entries.Enqueue(entry);

        // Trim old entries if we exceed max
        while (_entries.Count > MaxEntries && _entries.TryDequeue(out _))
        {
        }

        OnNewEntry?.Invoke(entry);
    }

    public void LogInfo(string streamId, string streamName, string message)
    {
        Log(new ActivityEntry
        {
            Id = Guid.NewGuid().ToString("N")[..8],
            Timestamp = DateTime.UtcNow,
            Level = ActivityLevel.Info,
            StreamId = streamId,
            StreamName = streamName,
            Message = message
        });
    }

    public void LogSuccess(string streamId, string streamName, string message, decimal? amount = null)
    {
        Log(new ActivityEntry
        {
            Id = Guid.NewGuid().ToString("N")[..8],
            Timestamp = DateTime.UtcNow,
            Level = ActivityLevel.Success,
            StreamId = streamId,
            StreamName = streamName,
            Message = message,
            Amount = amount
        });
    }

    public void LogWarning(string streamId, string streamName, string message)
    {
        Log(new ActivityEntry
        {
            Id = Guid.NewGuid().ToString("N")[..8],
            Timestamp = DateTime.UtcNow,
            Level = ActivityLevel.Warning,
            StreamId = streamId,
            StreamName = streamName,
            Message = message
        });
    }

    public void LogError(string streamId, string streamName, string message, string? details = null)
    {
        Log(new ActivityEntry
        {
            Id = Guid.NewGuid().ToString("N")[..8],
            Timestamp = DateTime.UtcNow,
            Level = ActivityLevel.Error,
            StreamId = streamId,
            StreamName = streamName,
            Message = message,
            Details = details
        });
    }

    public void Clear()
    {
        while (_entries.TryDequeue(out _))
        {
        }
    }
}
