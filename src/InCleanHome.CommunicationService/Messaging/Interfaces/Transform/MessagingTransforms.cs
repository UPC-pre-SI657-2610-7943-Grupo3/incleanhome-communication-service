using InCleanHome.CommunicationService.Messaging.Domain.Model.Aggregates;
using InCleanHome.CommunicationService.Messaging.Domain.Model.Queries;
using InCleanHome.CommunicationService.Messaging.Interfaces.Resources;

namespace InCleanHome.CommunicationService.Messaging.Interfaces.Transform;

public static class MessageResourceFromEntityAssembler
{
    public static MessageResource ToResourceFromEntity(Message m)
        => new(m.Id, m.SenderId, m.RecipientId, m.Content, m.CreatedDate, m.ReadAt);
}

public static class ConversationResourceFromViewAssembler
{
    public static ConversationResource ToResourceFromView(ConversationView v)
        => new(v.UserId, v.UserName, v.UserPhotoUrl, v.LastMessage, v.LastMessageAt, v.UnreadCount);
}
