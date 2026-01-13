using FluentResults;
using Income.Application.Services.Notifications;
using Income.Infrastructure.Persistence;
using Income.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Income.Infrastructure.Services;

internal sealed class NotificationService(IDbContextFactory<IncomeDbContext> dbContextFactory) : INotificationService
{
    public async Task<Result<IReadOnlyList<NotificationItem>>> GetAllAsync(int limit = 50, CancellationToken ct = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var entities = await dbContext.Notifications
            .OrderByDescending(x => x.CreatedAt)
            .Take(limit)
            .AsNoTracking()
            .ToListAsync(ct);

        var items = entities.Select(MapToItem).ToList();
        return Result.Ok<IReadOnlyList<NotificationItem>>(items);
    }

    public async Task<Result<IReadOnlyList<NotificationItem>>> GetUnreadAsync(CancellationToken ct = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var entities = await dbContext.Notifications
            .Where(x => !x.IsRead)
            .OrderByDescending(x => x.CreatedAt)
            .AsNoTracking()
            .ToListAsync(ct);

        var items = entities.Select(MapToItem).ToList();
        return Result.Ok<IReadOnlyList<NotificationItem>>(items);
    }

    public async Task<Result<int>> GetUnreadCountAsync(CancellationToken ct = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var count = await dbContext.Notifications
            .CountAsync(x => !x.IsRead, ct);

        return Result.Ok(count);
    }

    public async Task<Result> MarkAsReadAsync(string notificationId, CancellationToken ct = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var entity = await dbContext.Notifications
            .FirstOrDefaultAsync(x => x.Id == notificationId, ct);

        if (entity is null)
            return Result.Fail($"Notification with id '{notificationId}' not found");

        entity.IsRead = true;
        entity.ReadAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(ct);

        return Result.Ok();
    }

    public async Task<Result> MarkAllAsReadAsync(CancellationToken ct = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        await dbContext.Notifications
            .Where(x => !x.IsRead)
            .ExecuteUpdateAsync(x => x
                .SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.ReadAt, DateTime.UtcNow), ct);

        return Result.Ok();
    }

    public async Task<Result> CreateAsync(CreateNotificationRequest request, CancellationToken ct = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var entity = new NotificationEntity
        {
            Id = Guid.NewGuid().ToString(),
            Type = request.Type,
            Title = request.Title,
            Message = request.Message,
            StreamId = request.StreamId,
            StreamName = request.StreamName,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        await dbContext.Notifications.AddAsync(entity, ct);
        await dbContext.SaveChangesAsync(ct);

        return Result.Ok();
    }

    public async Task<Result> DeleteOldNotificationsAsync(int daysToKeep = 30, CancellationToken ct = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);

        await dbContext.Notifications
            .Where(x => x.CreatedAt < cutoffDate && x.IsRead)
            .ExecuteDeleteAsync(ct);

        return Result.Ok();
    }

    public async Task<Result> ClearAllAsync(CancellationToken ct = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        await dbContext.Notifications.ExecuteDeleteAsync(ct);

        return Result.Ok();
    }

    private static NotificationItem MapToItem(NotificationEntity entity)
    {
        return new NotificationItem(
            Id: entity.Id,
            Type: entity.Type,
            Title: entity.Title,
            Message: entity.Message,
            StreamId: entity.StreamId,
            StreamName: entity.StreamName,
            IsRead: entity.IsRead,
            CreatedAt: entity.CreatedAt);
    }
}
