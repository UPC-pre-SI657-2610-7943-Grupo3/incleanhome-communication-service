using InCleanHome.CommunicationService.Infrastructure.Messaging.Events;
using InCleanHome.CommunicationService.Notifications.Domain.Model.Aggregates;
using InCleanHome.CommunicationService.Notifications.Domain.Repositories;
using InCleanHome.CommunicationService.Shared;
using MassTransit;

namespace InCleanHome.CommunicationService.Infrastructure.Messaging.Consumers;

/// <summary>
/// Maintains the local <c>UserDevice</c> projection. Whenever IAM publishes a
/// device-token update, we upsert it here. If the token is null/empty (user
/// logged out), we remove the row.
/// </summary>
public class UserDeviceTokenUpdatedConsumer(
    IUserDeviceRepository repository,
    IUnitOfWork unitOfWork,
    ILogger<UserDeviceTokenUpdatedConsumer> logger) : IConsumer<UserDeviceTokenUpdatedEvent>
{
    public async Task Consume(ConsumeContext<UserDeviceTokenUpdatedEvent> ctx)
    {
        var evt = ctx.Message;
        var existing = await repository.FindByUserIdAsync(evt.UserId);

        if (string.IsNullOrWhiteSpace(evt.Token))
        {
            if (existing is not null)
            {
                repository.Remove(existing);
                await unitOfWork.CompleteAsync();
                logger.LogInformation("[UserDeviceToken] cleared for userId={UserId}", evt.UserId);
            }
            return;
        }

        if (existing is null)
        {
            var device = new UserDevice(evt.UserId, evt.Token, evt.Role);
            await repository.AddAsync(device);
        }
        else
        {
            existing.UpdateToken(evt.Token);
            if (!string.IsNullOrEmpty(evt.Role)) existing.UpdateRole(evt.Role);
            repository.Update(existing);
        }
        await unitOfWork.CompleteAsync();
        logger.LogInformation("[UserDeviceToken] updated for userId={UserId}", evt.UserId);
    }
}
