namespace Income.Infrastructure.Persistence.Entities;

internal sealed class NotificationEntity
{
    public string Id { get; set; } = null!;
    public string Type { get; set; } = null!; // "SyncSuccess", "SyncError", "RecurringSuccess", "RecurringError"
    public string Title { get; set; } = null!;
    public string Message { get; set; } = null!;
    public string? StreamId { get; set; }
    public string? StreamName { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
}
