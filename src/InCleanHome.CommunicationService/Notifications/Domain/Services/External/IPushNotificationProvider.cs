namespace InCleanHome.CommunicationService.Notifications.Domain.Services.External;

/// <summary>
/// Domain port for a push notification provider.
/// Today only FCM (Firebase). The adapter lives in Infrastructure/Firebase.
/// </summary>
public interface IPushNotificationProvider
{
    /// <summary>
    /// Sends a push notification to a single device token.
    /// Returns the provider's message id (for tracing).
    /// </summary>
    Task<string> SendNotificationAsync(string deviceToken, string title, string body,
        Dictionary<string, string>? data = null);
}
