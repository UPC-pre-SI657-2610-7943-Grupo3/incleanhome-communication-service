using System.Net.Mime;
using InCleanHome.CommunicationService.Notifications.Domain.Model.Aggregates;
using InCleanHome.CommunicationService.Notifications.Domain.Repositories;
using InCleanHome.CommunicationService.Shared;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace InCleanHome.CommunicationService.Notifications.Interfaces.Controllers;

/// <summary>
///   Internal endpoint used by IAM Service to keep the local
///   <c>UserDevice</c> projection in sync WITHOUT going through the RabbitMQ
///   broker. This is critical for FCM push to actually work:
/// </summary>
/// <remarks>
///   <para>
///     When the frontend logs in and requests notification permission, it
///     obtains an FCM device token and POSTs it to <c>iam-service</c> at
///     <c>/api/v1/auth/device-token</c>. IAM stores it on <c>users.device_token</c>
///     and publishes <c>UserDeviceTokenUpdatedEvent</c> for
///     <c>communication-service</c> to project locally.
///   </para>
///   <para>
///     Problem: if the broker drops or delays that event, comm-service's
///     <c>user_devices</c> table never gets populated. When the next in-app
///     notification is created, <c>FindTokenByUserIdAsync</c> returns null
///     and the FCM push step is silently skipped — the user reported exactly
///     this: "notificaciones in-app aparecen, pero push FCM no".
///   </para>
///   <para>
///     Fix: IAM now ALSO calls this HTTP endpoint immediately after
///     persisting the token. This is a straight upsert; the broker event
///     still fires as a redundant path.
///   </para>
///   <para>
///     Security: not exposed via API gateway. Requires the same
///     <c>X-Internal-Token</c> header as the rest of the internal endpoints.
///   </para>
/// </remarks>
[ApiController]
[Route("api/v1/internal/user-devices")]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerTag("Internal — service-to-service user device sync")]
public class InternalUserDevicesController(
    IUserDeviceRepository userDeviceRepository,
    IUnitOfWork unitOfWork,
    IConfiguration configuration,
    ILogger<InternalUserDevicesController> logger) : ControllerBase
{
    /// <summary>
    ///   Upsert the FCM token for a user. If token is null/empty, we clear it
    ///   (used when the user logs out on this device).
    /// </summary>
    [HttpPost]
    [SwaggerOperation("Upsert user device token (internal)")]
    public async Task<IActionResult> Upsert([FromBody] UpsertUserDeviceBody body)
    {
        if (!IsAuthorized())
        {
            logger.LogWarning(
                "[internal/user-devices] Unauthorized upsert for user {UserId}",
                body?.UserId);
            return Unauthorized(new { error = "Invalid or missing internal token" });
        }

        if (body is null || body.UserId <= 0)
            return BadRequest(new { error = "userId is required" });

        var existing = await userDeviceRepository.FindByUserIdAsync(body.UserId);

        if (string.IsNullOrWhiteSpace(body.Token))
        {
            // Clear (logout).
            if (existing is not null)
            {
                userDeviceRepository.Remove(existing);
                await unitOfWork.CompleteAsync();
                logger.LogInformation(
                    "[internal/user-devices] cleared for userId={UserId}", body.UserId);
            }
            return Ok(new { userId = body.UserId, cleared = true });
        }

        if (existing is null)
        {
            var device = new UserDevice(body.UserId, body.Token, body.Role);
            await userDeviceRepository.AddAsync(device);
        }
        else
        {
            existing.UpdateToken(body.Token);
            if (!string.IsNullOrEmpty(body.Role)) existing.UpdateRole(body.Role);
            userDeviceRepository.Update(existing);
        }
        await unitOfWork.CompleteAsync();

        logger.LogInformation(
            "[internal/user-devices] upserted for userId={UserId} role={Role}",
            body.UserId, body.Role);

        return Ok(new { userId = body.UserId, upserted = true });
    }

    private bool IsAuthorized()
    {
        var expected = Environment.GetEnvironmentVariable("INTERNAL_SERVICE_TOKEN")
                       ?? configuration["Internal:ServiceToken"];
        if (string.IsNullOrEmpty(expected)) return true;
        var got = Request.Headers["X-Internal-Token"].FirstOrDefault();
        return string.Equals(got, expected, StringComparison.Ordinal);
    }
}

public record UpsertUserDeviceBody(int UserId, string? Token, string? Role);
