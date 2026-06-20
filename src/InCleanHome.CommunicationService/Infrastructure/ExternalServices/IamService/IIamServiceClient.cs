using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace InCleanHome.CommunicationService.Infrastructure.ExternalServices.IamService;

/// <summary>
/// HTTP client to talk to IAM Service. Used to fetch a user's role (for
/// notification link routing) and their FCM device token (for push).
/// </summary>
public interface IIamServiceClient
{
    Task<UserSummary?> GetUserAsync(int userId, string bearerToken);
}

public record UserSummary(int Id, string Email, string Role, string? DeviceToken);

public class IamServiceClient(
    HttpClient http,
    IConfiguration configuration,
    ILogger<IamServiceClient> logger) : IIamServiceClient
{
    private string BaseUrl => configuration["Dependencies:IamServiceUrl"] ?? "http://iam-service:5001";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Calls GET /api/admin/users to fetch all users and filters by id.
    /// Uses an admin-level token (we need the deviceToken which is not exposed
    /// to non-admin users). The admin token comes from a service-to-service
    /// JWT (the gateway's signing key) signed for an admin identity.
    /// </summary>
    public async Task<UserSummary?> GetUserAsync(int userId, string bearerToken)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/api/admin/users");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

            using var resp = await http.SendAsync(req);
            if (!resp.IsSuccessStatusCode)
            {
                logger.LogWarning("GET /api/admin/users -> {Status}", resp.StatusCode);
                return null;
            }

            var arr = await resp.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
            if (arr.ValueKind != JsonValueKind.Array) return null;

            foreach (var el in arr.EnumerateArray())
            {
                var id = el.TryGetProperty("id", out var idEl) ? idEl.GetInt32() : 0;
                if (id != userId) continue;
                return new UserSummary(
                    id,
                    el.TryGetProperty("email", out var e1) ? e1.GetString() ?? "" : "",
                    el.TryGetProperty("role",  out var e2) ? e2.GetString() ?? "" : "",
                    el.TryGetProperty("deviceToken", out var e3) ? e3.GetString() : null);
            }
            return null;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "GetUserAsync failed for {Id}", userId);
            return null;
        }
    }
}
