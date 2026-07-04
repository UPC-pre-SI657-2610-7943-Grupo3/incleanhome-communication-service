using InCleanHome.CommunicationService.Notifications.Domain.Model.Aggregates;
using InCleanHome.CommunicationService.Notifications.Domain.Model.Commands;
using InCleanHome.CommunicationService.Notifications.Domain.Repositories;
using InCleanHome.CommunicationService.Notifications.Domain.Services;
using InCleanHome.CommunicationService.Notifications.Domain.Services.External;
using InCleanHome.CommunicationService.Shared;

namespace InCleanHome.CommunicationService.Notifications.Application.CommandServices;

public class NotificationCommandService(
    INotificationRepository repository,
    IUserDeviceRepository userDeviceRepository,
    IUnitOfWork unitOfWork,
    IPushNotificationProvider pushProvider,
    ILogger<NotificationCommandService> logger) : INotificationCommandService
{
    public Task<Notification> Handle(CreateNotificationCommand c)
        => HandleWithIdempotency(c, idempotencyKey: null);

    public async Task<Notification> HandleWithIdempotency(CreateNotificationCommand c, string? idempotencyKey)
    {
        // 1. Persist in-app notification (history in the web app).
        var notification = new Notification(c.UserId, c.Type, c.Title, c.Body, c.Link, idempotencyKey);
        await repository.AddAsync(notification);
        await unitOfWork.CompleteAsync();

        logger.LogInformation(
            "[Notifications] Created id={Id} for userId={UserId} type={Type} idemKey={Key}",
            notification.Id, c.UserId, c.Type, idempotencyKey ?? "(none)");

        // 2. Push via Firebase. Best-effort: a failure here MUST NOT break the
        //    transaction. The in-app notification is already saved.
        var deviceToken = await userDeviceRepository.FindTokenByUserIdAsync(c.UserId);

        if (string.IsNullOrEmpty(deviceToken))
        {
            logger.LogInformation("[Notifications] No device token for userId={UserId}; skipping push", c.UserId);
            return notification;
        }

        try
        {
            var extra = new Dictionary<string, string>
            {
                { "type",           c.Type },
                { "link",           c.Link ?? "" },
                { "userId",         c.UserId.ToString() },
                { "notificationId", notification.Id.ToString() }
            };
            await pushProvider.SendNotificationAsync(deviceToken, c.Title, c.Body, extra);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "[Firebase Error] Could not send push to userId={UserId}", c.UserId);
        }

        return notification;
    }

    public async Task<bool> Handle(MarkNotificationReadCommand c)
    {
        var notification = await repository.FindByIdAsync(c.NotificationId);
        if (notification is null || notification.UserId != c.UserId) return false;
        notification.MarkAsRead();
        repository.Update(notification);
        await unitOfWork.CompleteAsync();
        return true;
    }

    public async Task Handle(MarkAllNotificationsReadCommand c)
    {
        var unread = await repository.FindUnreadByUserIdAsync(c.UserId);
        foreach (var n in unread)
        {
            n.MarkAsRead();
            repository.Update(n);
        }
        await unitOfWork.CompleteAsync();
    }

    public async Task<bool> Handle(DeleteNotificationCommand c)
    {
        var notification = await repository.FindByIdAsync(c.NotificationId);
        if (notification is null || notification.UserId != c.UserId) return false;
        repository.Remove(notification);
        await unitOfWork.CompleteAsync();
        return true;
    }
}
