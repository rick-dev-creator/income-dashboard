using FluentResults;

namespace Income.Application.Services.Notifications;

public interface INotificationService
{
    Task<Result<IReadOnlyList<NotificationItem>>> GetAllAsync(int limit = 50, CancellationToken ct = default);
    Task<Result<IReadOnlyList<NotificationItem>>> GetUnreadAsync(CancellationToken ct = default);
    Task<Result<int>> GetUnreadCountAsync(CancellationToken ct = default);
    Task<Result> MarkAsReadAsync(string notificationId, CancellationToken ct = default);
    Task<Result> MarkAllAsReadAsync(CancellationToken ct = default);
    Task<Result> CreateAsync(CreateNotificationRequest request, CancellationToken ct = default);
    Task<Result> DeleteOldNotificationsAsync(int daysToKeep = 30, CancellationToken ct = default);
    Task<Result> ClearAllAsync(CancellationToken ct = default);
}

// DTOs
public sealed record NotificationItem(
    string Id,
    string Type,
    string Title,
    string Message,
    string? StreamId,
    string? StreamName,
    bool IsRead,
    DateTime CreatedAt);

public sealed record CreateNotificationRequest(
    string Type,
    string Title,
    string Message,
    string? StreamId = null,
    string? StreamName = null);

public static class NotificationTypes
{
    public const string SyncSuccess = "SyncSuccess";
    public const string SyncError = "SyncError";
    public const string RecurringSuccess = "RecurringSuccess";
    public const string RecurringError = "RecurringError";
}
