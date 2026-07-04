using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace InCleanHome.CommunicationService.Infrastructure.ExternalServices.IamService;

/// <summary>
/// HTTP client to talk to IAM Service. Used to fetch a user's role (for
/// notification link routing).
/// </summary>
public interface IIamServiceClient
{
    Task<UserSummary?> GetUserAsync(int userId, string bearerToken);
}

public record UserSummary(int Id, string Role);

public class IamServiceClient(
    HttpClient http,
    IConfiguration configuration,
    ILogger<IamServiceClient> logger) : IIamServiceClient
{
    private string BaseUrl => configuration["Dependencies:IamServiceUrl"] ?? "http://iam-service:5001";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Calls GET /api/v1/users/{id}/public-status (non-admin endpoint) to fetch
    /// the user's role. We only need the role here to choose the link path for
    /// the in-app notification (/worker/... vs /client/...).
    /// </summary>
    public async Task<UserSummary?> GetUserAsync(int userId, string bearerToken)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/api/v1/users/{userId}/public-status");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

            using var resp = await http.SendAsync(req);
            if (!resp.IsSuccessStatusCode)
            {
                logger.LogWarning("GET /api/v1/users/{Id}/public-status -> {Status}", userId, resp.StatusCode);
                return null;
            }

            var el = await resp.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
            var id = el.TryGetProperty("id", out var idEl) ? idEl.GetInt32() : 0;
            var role = el.TryGetProperty("role", out var rEl) ? rEl.GetString() ?? "" : "";
            return new UserSummary(id, role);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "GetUserAsync failed for {Id}", userId);
            return null;
        }
    }
}
