namespace InCleanHome.CommunicationService.Notifications.Interfaces.Resources;

public record NotificationResource(
    int Id,
    string Type,
    string Title,
    string Body,
    string? Link,
    bool Read,
    DateTimeOffset? CreatedAt);

public record UnreadCountResource(int Count);
