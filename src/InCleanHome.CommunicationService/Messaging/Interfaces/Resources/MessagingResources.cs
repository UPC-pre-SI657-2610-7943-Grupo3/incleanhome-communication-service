namespace InCleanHome.CommunicationService.Messaging.Interfaces.Resources;

public record SendMessageResource(string Content);

public record MessageResource(
    int Id,
    int SenderId,
    int RecipientId,
    string Content,
    DateTimeOffset? CreatedAt,
    DateTimeOffset? ReadAt);

public record ConversationResource(
    int UserId,
    string UserName,
    string? UserPhotoUrl,
    string LastMessage,
    DateTimeOffset? LastMessageAt,
    int UnreadCount);

public record TwilioTokenResource(string Token, string Identity);
public record ConversationSidResource(string ConversationSid);
