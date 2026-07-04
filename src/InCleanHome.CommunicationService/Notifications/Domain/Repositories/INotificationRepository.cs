using InCleanHome.CommunicationService.Notifications.Domain.Model.Aggregates;
using InCleanHome.CommunicationService.Notifications.Domain.Model.Commands;
using InCleanHome.CommunicationService.Notifications.Domain.Model.Queries;
using InCleanHome.CommunicationService.Shared;

namespace InCleanHome.CommunicationService.Notifications.Domain.Repositories;

public interface INotificationRepository : IBaseRepository<Notification>
{
    Task<IEnumerable<Notification>> FindByUserIdAsync(int userId);
    Task<int> CountUnreadByUserIdAsync(int userId);
    Task<IEnumerable<Notification>> FindUnreadByUserIdAsync(int userId);
    Task<Notification?> FindByIdempotencyKeyAsync(string idempotencyKey);
}
