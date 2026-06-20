using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using InCleanHome.CommunicationService.Notifications.Domain.Services.External;
using System.Text;
using System.Text.Json;

namespace InCleanHome.CommunicationService.Notifications.Infrastructure.Firebase;

/// <summary>
/// Concrete adapter for FCM implementing <see cref="IPushNotificationProvider"/>.
/// Initialization reads <c>firebase-service-account.json</c> (path configurable via env).
/// </summary>
public class FirebaseCloudMessagingAdapter : IPushNotificationProvider
{
    private const string DefaultCredentialsFileName = "firebase-service-account.json";

    private readonly ILogger<FirebaseCloudMessagingAdapter> _logger;

    public FirebaseCloudMessagingAdapter(IConfiguration configuration, ILogger<FirebaseCloudMessagingAdapter> logger)
    {
        _logger = logger;

        if (FirebaseApp.DefaultInstance != null) return;

        // Allow override via env var FIREBASE_CREDENTIALS_JSON (full path).
        var envPath = Environment.GetEnvironmentVariable("FIREBASE_CREDENTIALS_JSON");
        var candidatePaths = new List<string>();
        if (!string.IsNullOrWhiteSpace(envPath)) candidatePaths.Add(envPath);
        candidatePaths.AddRange(new[]
        {
            Path.Combine(AppContext.BaseDirectory, DefaultCredentialsFileName),
            Path.Combine(Directory.GetCurrentDirectory(), DefaultCredentialsFileName),
            DefaultCredentialsFileName
        });

        var credentialsPath = candidatePaths.FirstOrDefault(File.Exists);
        if (credentialsPath is null)
        {
            _logger.LogWarning(
                "[Firebase] No credentials file found. Looked at: {Paths}. Push notifications will fail.",
                string.Join(", ", candidatePaths));
            return;
        }

        try
        {
            var jsonContent = File.ReadAllText(credentialsPath);
            var keyData = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonContent);
            if (keyData == null || !keyData.ContainsKey("project_id"))
                throw new InvalidOperationException("Firebase credentials file is invalid (no project_id).");

            var jsonBytes = Encoding.UTF8.GetBytes(jsonContent);
            using var memoryStream = new MemoryStream(jsonBytes);

#pragma warning disable CS0618
            var googleCredential = GoogleCredential.FromStream(memoryStream)
                .CreateScoped("https://www.googleapis.com/auth/firebase.messaging");
#pragma warning restore CS0618

            FirebaseApp.Create(new AppOptions
            {
                Credential = googleCredential,
                ProjectId  = keyData["project_id"]
            });

            _logger.LogInformation(
                "[Firebase] Initialized with project '{Project}' from {Path}",
                keyData["project_id"], credentialsPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Firebase] Initialization failed. Push notifications will be disabled.");
        }
    }

    public async Task<string> SendNotificationAsync(string deviceToken, string title, string body,
        Dictionary<string, string>? data = null)
    {
        if (string.IsNullOrWhiteSpace(deviceToken))
            throw new ArgumentException("Device token is required.", nameof(deviceToken));

        if (FirebaseApp.DefaultInstance is null)
            throw new InvalidOperationException("Firebase is not initialized.");

        var message = new Message
        {
            Token        = deviceToken,
            Notification = new FirebaseAdmin.Messaging.Notification { Title = title, Body = body },
            Data         = data,
            Webpush      = new WebpushConfig
            {
                Notification = new WebpushNotification
                {
                    Title = title,
                    Body  = body,
                    Icon  = "/favicon.svg"
                },
                FcmOptions = data != null
                             && data.TryGetValue("link", out var link)
                             && !string.IsNullOrEmpty(link)
                             && (link.StartsWith("https://") || link.StartsWith("http://"))
                    ? new WebpushFcmOptions { Link = link }
                    : null
            }
        };

        var messageId = await FirebaseMessaging.DefaultInstance.SendAsync(message);
        _logger.LogInformation(
            "[Firebase] Push sent. messageId={MessageId} to token={TokenPrefix}...",
            messageId, deviceToken[..Math.Min(20, deviceToken.Length)]);
        return messageId;
    }
}
