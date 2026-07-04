using InCleanHome.CommunicationService.Infrastructure.Persistence;
using InCleanHome.CommunicationService.Notifications.Domain.Model.Aggregates;
using InCleanHome.CommunicationService.Notifications.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace InCleanHome.CommunicationService.Notifications.Infrastructure.Repositories;

public class NotificationRepository(CommunicationDbContext context)
    : BaseRepository<Notification>(context), INotificationRepository
{
    public async Task<IEnumerable<Notification>> FindByUserIdAsync(int userId)
        => await Context.Set<Notification>()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedDate)
            .ToListAsync();

    public async Task<int> CountUnreadByUserIdAsync(int userId)
        => await Context.Set<Notification>()
            .CountAsync(n => n.UserId == userId && !n.Read);

    public async Task<IEnumerable<Notification>> FindUnreadByUserIdAsync(int userId)
        => await Context.Set<Notification>()
            .Where(n => n.UserId == userId && !n.Read)
            .ToListAsync();

    public async Task<Notification?> FindByIdempotencyKeyAsync(string idempotencyKey)
        => await Context.Set<Notification>()
            .FirstOrDefaultAsync(n => n.IdempotencyKey == idempotencyKey);
}

public class UserDeviceRepository(CommunicationDbContext context)
    : BaseRepository<UserDevice>(context), IUserDeviceRepository
{
    public async Task<UserDevice?> FindByUserIdAsync(int userId)
        => await Context.Set<UserDevice>().FirstOrDefaultAsync(d => d.UserId == userId);

    public async Task<string?> FindTokenByUserIdAsync(int userId)
        => await Context.Set<UserDevice>()
            .Where(d => d.UserId == userId)
            .Select(d => d.Token)
            .FirstOrDefaultAsync();

    public async Task<string?> FindRoleByUserIdAsync(int userId)
        => await Context.Set<UserDevice>()
            .Where(d => d.UserId == userId)
            .Select(d => d.Role)
            .FirstOrDefaultAsync();
}
