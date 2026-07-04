using InCleanHome.CommunicationService.Notifications.Domain.Model.Aggregates;
using InCleanHome.CommunicationService.Notifications.Domain.Model.Commands;
using InCleanHome.CommunicationService.Notifications.Domain.Model.Queries;

namespace InCleanHome.CommunicationService.Notifications.Domain.Services;

public interface INotificationCommandService
{
    Task<Notification> Handle(CreateNotificationCommand command);
    /// <summary>
    ///   Same as <see cref="Handle(CreateNotificationCommand)"/> but stores
    ///   the given <paramref name="idempotencyKey"/> on the resulting
    ///   notification. Used by the internal HTTP endpoint so service-to-service
    ///   creates can be retried safely.
    /// </summary>
    Task<Notification> HandleWithIdempotency(CreateNotificationCommand command, string? idempotencyKey);
    Task<bool> Handle(MarkNotificationReadCommand command);
    Task Handle(MarkAllNotificationsReadCommand command);
    Task<bool> Handle(DeleteNotificationCommand command);
}

public interface INotificationQueryService
{
    Task<IEnumerable<Notification>> Handle(GetNotificationsByUserIdQuery query);
    Task<int> Handle(GetUnreadCountByUserIdQuery query);
}
