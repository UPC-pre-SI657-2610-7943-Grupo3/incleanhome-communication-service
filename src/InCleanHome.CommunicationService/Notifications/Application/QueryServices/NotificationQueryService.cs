using InCleanHome.CommunicationService.Notifications.Domain.Model.Aggregates;
using InCleanHome.CommunicationService.Notifications.Domain.Model.Queries;
using InCleanHome.CommunicationService.Notifications.Domain.Repositories;
using InCleanHome.CommunicationService.Notifications.Domain.Services;

namespace InCleanHome.CommunicationService.Notifications.Application.QueryServices;

public class NotificationQueryService(INotificationRepository repository) : INotificationQueryService
{
    public async Task<IEnumerable<Notification>> Handle(GetNotificationsByUserIdQuery query)
        => await repository.FindByUserIdAsync(query.UserId);

    public async Task<int> Handle(GetUnreadCountByUserIdQuery query)
        => await repository.CountUnreadByUserIdAsync(query.UserId);
}
