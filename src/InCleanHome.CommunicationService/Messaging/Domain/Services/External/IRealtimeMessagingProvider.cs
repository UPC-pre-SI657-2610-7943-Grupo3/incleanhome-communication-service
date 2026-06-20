namespace InCleanHome.CommunicationService.Messaging.Domain.Services.External;

/// <summary>
/// Domain port for a real-time messaging provider (today: Twilio Conversations).
/// </summary>
public interface IRealtimeMessagingProvider
{
    /// <summary>Returns the SID of the conversation between two users (creates if missing).</summary>
    Task<string> GetOrCreateConversationSidAsync(string participantA, string participantB);

    /// <summary>Generates a short-lived access token so the frontend can connect to the SDK.</summary>
    string GenerateAccessToken(string identity);
}
