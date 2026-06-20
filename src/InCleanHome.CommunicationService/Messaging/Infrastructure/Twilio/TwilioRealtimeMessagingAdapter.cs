using InCleanHome.CommunicationService.Messaging.Domain.Services.External;
using Twilio;
using Twilio.Jwt.AccessToken;
using Twilio.Rest.Conversations.V1.Service;
using Twilio.Rest.Conversations.V1.Service.Conversation;

namespace InCleanHome.CommunicationService.Messaging.Infrastructure.Twilio;

/// <summary>
/// Twilio Conversations adapter for <see cref="IRealtimeMessagingProvider"/>.
/// Only this class knows about Twilio's SDK details.
/// </summary>
public class TwilioRealtimeMessagingAdapter : IRealtimeMessagingProvider
{
    private readonly string _accountSid;
    private readonly string _apiKeySid;
    private readonly string _apiKeySecret;
    private readonly string _conversationServiceSid;
    private readonly bool _initialized;
    private readonly ILogger<TwilioRealtimeMessagingAdapter> _logger;

    public TwilioRealtimeMessagingAdapter(
        IConfiguration configuration,
        ILogger<TwilioRealtimeMessagingAdapter> logger)
    {
        _logger = logger;

        _accountSid             = configuration["Twilio:AccountSid"] ?? string.Empty;
        _apiKeySid              = configuration["Twilio:ApiKeySid"] ?? string.Empty;
        _apiKeySecret           = configuration["Twilio:ApiKeySecret"] ?? string.Empty;
        _conversationServiceSid = configuration["Twilio:ConversationServiceSid"] ?? string.Empty;

        var authToken = configuration["Twilio:AuthToken"];

        if (string.IsNullOrWhiteSpace(_accountSid) || string.IsNullOrWhiteSpace(authToken))
        {
            _logger.LogWarning("[Twilio] Not configured. Chat endpoints will fail.");
            _initialized = false;
            return;
        }

        TwilioClient.Init(_accountSid, authToken);
        _initialized = true;
    }

    public async Task<string> GetOrCreateConversationSidAsync(string participantA, string participantB)
    {
        EnsureInitialized();

        var ids = new[] { participantA, participantB }.OrderBy(x => x).ToArray();
        var uniqueName = $"incleanhome_{ids[0]}_{ids[1]}";

        try
        {
            var createOptions = new CreateConversationOptions(_conversationServiceSid)
            {
                UniqueName   = uniqueName,
                FriendlyName = $"Chat {participantA} - {participantB}"
            };
            var conversation = await ConversationResource.CreateAsync(createOptions);

            await AddParticipantIfNotExists(conversation.Sid, participantA);
            await AddParticipantIfNotExists(conversation.Sid, participantB);

            return conversation.Sid;
        }
        catch (global::Twilio.Exceptions.ApiException)
        {
            // Already exists; look it up.
            var readOptions = new ReadConversationOptions(_conversationServiceSid);
            var conversations = await ConversationResource.ReadAsync(readOptions);
            var existing = conversations.FirstOrDefault(c => c.UniqueName == uniqueName);
            if (existing != null) return existing.Sid;
            throw;
        }
    }

    public string GenerateAccessToken(string identity)
    {
        EnsureInitialized();

        var grant  = new ChatGrant { ServiceSid = _conversationServiceSid };
        var grants = new HashSet<IGrant> { grant };

        var token = new Token(
            accountSid:    _accountSid,
            signingKeySid: _apiKeySid,
            secret:        _apiKeySecret,
            identity:      identity,
            expiration:    DateTime.UtcNow.AddHours(24),
            grants:        grants);

        return token.ToJwt();
    }

    private async Task AddParticipantIfNotExists(string conversationSid, string identity)
    {
        try
        {
            var options = new CreateParticipantOptions(_conversationServiceSid, conversationSid)
            {
                Identity = identity
            };
            await ParticipantResource.CreateAsync(options);
        }
        catch
        {
            // Already exists — ignore.
        }
    }

    private void EnsureInitialized()
    {
        if (!_initialized)
            throw new InvalidOperationException(
                "Twilio is not configured. Set TWILIO_* env vars and restart.");
    }
}
