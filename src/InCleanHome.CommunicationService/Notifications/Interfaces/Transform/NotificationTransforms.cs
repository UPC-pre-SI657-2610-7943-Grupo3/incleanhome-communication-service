using InCleanHome.CommunicationService.Notifications.Domain.Model.Aggregates;
using InCleanHome.CommunicationService.Notifications.Interfaces.Resources;

namespace InCleanHome.CommunicationService.Notifications.Interfaces.Transform;

public static class NotificationResourceFromEntityAssembler
{
    public static NotificationResource ToResourceFromEntity(Notification n)
        => new(n.Id, n.Type, n.Title, n.Body, n.Link, n.Read, n.CreatedDate);
}
