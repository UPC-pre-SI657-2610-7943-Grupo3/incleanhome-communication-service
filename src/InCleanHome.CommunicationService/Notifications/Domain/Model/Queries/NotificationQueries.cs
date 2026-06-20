namespace InCleanHome.CommunicationService.Notifications.Domain.Model.Queries;

public record GetNotificationsByUserIdQuery(int UserId);
public record GetUnreadCountByUserIdQuery(int UserId);
