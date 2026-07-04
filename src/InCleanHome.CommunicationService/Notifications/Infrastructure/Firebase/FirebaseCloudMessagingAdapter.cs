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
        // Some deployments mount the file at /app/firebase-credentials.json,
        // others bake it into the image alongside the .csproj.
        var envPath = Environment.GetEnvironmentVariable("FIREBASE_CREDENTIALS_JSON");

        // Look at several reasonable locations in order. We accept either
        // 'firebase-service-account.json' or 'firebase-credentials.json' since
        // both names exist in different parts of this project.
        var fileNames = new[] { DefaultCredentialsFileName, "firebase-credentials.json" };
        var directories = new[]
        {
            AppContext.BaseDirectory,           // /app inside the container after publish
            Directory.GetCurrentDirectory(),    // process CWD (usually /app too)
            "/app",                             // explicit container path
            Path.Combine(AppContext.BaseDirectory, ".."), // dev: ../publish
            ""                                  // bare filename relative to CWD
        };

        var candidatePaths = new List<string>();
        if (!string.IsNullOrWhiteSpace(envPath)) candidatePaths.Add(envPath);
        foreach (var dir in directories)
            foreach (var name in fileNames)
                candidatePaths.Add(string.IsNullOrEmpty(dir) ? name : Path.Combine(dir, name));

        string? credentialsPath = null;
        foreach (var path in candidatePaths)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path)) continue;
                if (Directory.Exists(path))
                {
                    // This is the exact failure mode the user reported:
                    // 'firebase-service-account.json' was committed as a
                    // directory instead of a file. Flag it loud and clear so
                    // it's not silently ignored.
                    _logger.LogError(
                        "[Firebase] Found a DIRECTORY at '{Path}' where a JSON " +
                        "file was expected. Replace the empty folder with the " +
                        "real service-account JSON downloaded from Firebase Console " +
                        "(Project Settings → Service Accounts → Generate new private key).",
                        path);
                    continue;
                }
                if (File.Exists(path)) { credentialsPath = path; break; }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "[Firebase] Skipping candidate {Path}", path);
            }
        }

        if (credentialsPath is null)
        {
            _logger.LogWarning(
                "[Firebase] No credentials FILE found. Looked at: {Paths}. " +
                "Push notifications will be disabled until you place a valid " +
                "service-account JSON at one of those paths (or point " +
                "FIREBASE_CREDENTIALS_JSON to it).",
                string.Join(" | ", candidatePaths));
            return;
        }

        try
        {
            var jsonContent = File.ReadAllText(credentialsPath);
            // The previous deserialize-to-Dictionary<string,string> only worked
            // because every value happened to be a string. With JsonDocument we
            // get a cleaner check that's robust to numeric fields and avoids the
            // brittle string-coercion path.
            using var doc = JsonDocument.Parse(jsonContent);
            if (!doc.RootElement.TryGetProperty("project_id", out var projectIdElement)
                || projectIdElement.ValueKind != JsonValueKind.String
                || string.IsNullOrWhiteSpace(projectIdElement.GetString()))
                throw new InvalidOperationException(
                    "Firebase credentials file is invalid: missing 'project_id'.");

            var projectId = projectIdElement.GetString()!;

            var jsonBytes = Encoding.UTF8.GetBytes(jsonContent);
            using var memoryStream = new MemoryStream(jsonBytes);

#pragma warning disable CS0618
            var googleCredential = GoogleCredential.FromStream(memoryStream)
                .CreateScoped("https://www.googleapis.com/auth/firebase.messaging");
#pragma warning restore CS0618

            FirebaseApp.Create(new AppOptions
            {
                Credential = googleCredential,
                ProjectId  = projectId
            });

            _logger.LogInformation(
                "[Firebase] Initialized with project '{Project}' from {Path}",
                projectId, credentialsPath);
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
