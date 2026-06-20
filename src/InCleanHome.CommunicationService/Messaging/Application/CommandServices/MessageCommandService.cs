using InCleanHome.CommunicationService.Infrastructure.ExternalServices.IamService;
using InCleanHome.CommunicationService.Infrastructure.ExternalServices.ProfileService;
using InCleanHome.CommunicationService.Messaging.Domain.Model.Aggregates;
using InCleanHome.CommunicationService.Messaging.Domain.Model.Commands;
using InCleanHome.CommunicationService.Messaging.Domain.Repositories;
using InCleanHome.CommunicationService.Messaging.Domain.Services;
using InCleanHome.CommunicationService.Notifications.Domain.Model.Commands;
using InCleanHome.CommunicationService.Notifications.Domain.Services;
using InCleanHome.CommunicationService.Shared;

namespace InCleanHome.CommunicationService.Messaging.Application.CommandServices;

/// <summary>
/// Message command service. After persisting the message locally, it creates
/// an in-app notification + push for the recipient using the SAME service
/// (NotificationCommandService) — both BCs live in the same microservice so
/// no HTTP round-trip is needed.
/// </summary>
public class MessageCommandService(
    IMessageRepository repository,
    IUnitOfWork unitOfWork,
    INotificationCommandService notificationCommandService,
    IIamServiceClient iamClient,
    IProfileServiceClient profileClient,
    IHttpContextAccessor httpContextAccessor,
    ILogger<MessageCommandService> logger) : IMessageCommandService
{
    public async Task<Message> Handle(SendMessageCommand c)
    {
        if (c.SenderId == c.RecipientId)
            throw new Exception("Cannot send a message to yourself.");
        if (string.IsNullOrWhiteSpace(c.Content))
            throw new Exception("Message content cannot be empty.");

        var message = new Message(c.SenderId, c.RecipientId, c.Content);
        await repository.AddAsync(message);
        await unitOfWork.CompleteAsync();

        // Best-effort notification + push to the recipient.
        try
        {
            var bearer = GetBearer();
            var recipient = await iamClient.GetUserAsync(c.RecipientId, bearer);
            var rolePath = recipient?.Role == "worker" ? "worker" : "client";
            var link = $"/{rolePath}/messages/{c.SenderId}";

            var senderPerson = await profileClient.GetAnyPersonAsync(c.SenderId, bearer);
            var senderName = string.IsNullOrWhiteSpace(senderPerson?.Name) ? "Alguien" : senderPerson!.Name;

            var preview = c.Content.Length > 120 ? c.Content[..117] + "..." : c.Content;

            await notificationCommandService.Handle(new CreateNotificationCommand(
                UserId: c.RecipientId,
                Type:   "message",
                Title:  $"{senderName} te envió un mensaje",
                Body:   preview,
                Link:   link));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[Messaging] Could not create notification for message");
        }

        return message;
    }

    public async Task Handle(MarkConversationAsReadCommand c)
    {
        await repository.MarkAsReadAsync(c.UserId, c.OtherUserId);
        await unitOfWork.CompleteAsync();
    }

    private string GetBearer()
    {
        var http = httpContextAccessor.HttpContext;
        if (http is null) return string.Empty;
        var raw = http.Request.Headers["Authorization"].FirstOrDefault() ?? string.Empty;
        return raw.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? raw["Bearer ".Length..] : raw;
    }
}
