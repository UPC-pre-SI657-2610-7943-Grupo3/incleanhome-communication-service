using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace InCleanHome.CommunicationService.Infrastructure.ExternalServices.ProfileService;

public interface IProfileServiceClient
{
    Task<ProfilePerson?> GetClientPersonAsync(int userId, string bearerToken);
    Task<ProfilePerson?> GetWorkerPersonAsync(int userId, string bearerToken);
    Task<ProfilePerson?> GetAnyPersonAsync(int userId, string bearerToken);
}

public record ProfilePerson(string Name, string? PhotoUrl);

public class ProfileServiceClient(
    HttpClient http,
    IConfiguration configuration,
    ILogger<ProfileServiceClient> logger) : IProfileServiceClient
{
    private string BaseUrl => configuration["Dependencies:ProfileServiceUrl"]
                              ?? "http://profile-service:5002";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task<ProfilePerson?> GetClientPersonAsync(int userId, string bearerToken)
        => GetPersonAsync($"{BaseUrl}/api/v1/profiles/clients/{userId}", bearerToken);

    public Task<ProfilePerson?> GetWorkerPersonAsync(int userId, string bearerToken)
        => GetPersonAsync($"{BaseUrl}/api/v1/profiles/workers/{userId}", bearerToken);

    public async Task<ProfilePerson?> GetAnyPersonAsync(int userId, string bearerToken)
        => await GetWorkerPersonAsync(userId, bearerToken)
        ?? await GetClientPersonAsync(userId, bearerToken);

    private async Task<ProfilePerson?> GetPersonAsync(string url, string bearerToken)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

            using var resp = await http.SendAsync(req);
            if (!resp.IsSuccessStatusCode) return null;

            var json = await resp.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
            var name = json.TryGetProperty("name", out var n) ? n.GetString() ?? string.Empty : string.Empty;
            var photo = json.TryGetProperty("photoUrl", out var p) ? p.GetString() : null;
            return new ProfilePerson(name, photo);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "GetPersonAsync failed for {Url}", url);
            return null;
        }
    }
}
