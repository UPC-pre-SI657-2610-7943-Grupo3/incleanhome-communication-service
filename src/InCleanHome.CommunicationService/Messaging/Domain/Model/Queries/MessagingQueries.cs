namespace InCleanHome.CommunicationService.Messaging.Domain.Model.Queries;

public record GetConversationsForUserQuery(int UserId);
public record GetMessagesBetweenQuery(int UserAId, int UserBId);

public record ConversationView(
    int UserId,
    string UserName,
    string? UserPhotoUrl,
    string LastMessage,
    DateTimeOffset? LastMessageAt,
    int UnreadCount);
