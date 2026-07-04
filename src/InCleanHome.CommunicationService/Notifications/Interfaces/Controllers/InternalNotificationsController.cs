using System.Net.Mime;
using InCleanHome.CommunicationService.Notifications.Domain.Model.Aggregates;
using InCleanHome.CommunicationService.Notifications.Domain.Model.Commands;
using InCleanHome.CommunicationService.Notifications.Domain.Services;
using InCleanHome.CommunicationService.Notifications.Domain.Repositories;
using InCleanHome.CommunicationService.Notifications.Interfaces.Transform;
using InCleanHome.CommunicationService.Shared;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace InCleanHome.CommunicationService.Notifications.Interfaces.Controllers;

/// <summary>
///   Internal service-to-service endpoint used by other microservices to
///   create a notification directly, bypassing the RabbitMQ pub-sub path.
/// </summary>
/// <remarks>
///   <para>
///     Background: in the monolith, every place that needed to notify a
///     user called <c>INotificationsContextFacade.CreateNotification(...)</c>
///     in-process. In microservices we kept the same semantics via RabbitMQ
///     events. That works when the broker is healthy but the user reported
///     in-app notifications never appearing — meaning the broker is either
///     unreachable, mis-routed, or events are being dropped silently.
///   </para>
///   <para>
///     This HTTP fallback gives services a reliable path: after they emit
///     to the broker (best-effort), they ALSO POST here. This endpoint
///     dedups by <c>IdempotencyKey</c>, so even if the broker eventually
///     delivers the same event the user gets exactly one notification.
///   </para>
///   <para>
///     Security: not exposed via the API gateway. Requires the same
///     <c>X-Internal-Token</c> header pattern as Profile Service.
///   </para>
/// </remarks>
[ApiController]
[Route("api/v1/internal/notifications")]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerTag("Internal — service-to-service notifications")]
public class InternalNotificationsController(
    INotificationCommandService commandService,
    INotificationRepository notificationRepository,
    IConfiguration configuration,
    ILogger<InternalNotificationsController> logger) : ControllerBase
{
    /// <summary>
    ///   Creates a notification (in-app + FCM) on behalf of another microservice.
    ///   Idempotent by <c>IdempotencyKey</c>: if the same key was used before
    ///   for the same user, this becomes a no-op and returns the existing
    ///   notification id.
    /// </summary>
    [HttpPost]
    [SwaggerOperation("Create notification (internal)")]
    public async Task<IActionResult> Create([FromBody] CreateNotificationInternalBody body)
    {
        if (!IsAuthorized())
        {
            logger.LogWarning(
                "[internal/notifications] Unauthorized create attempt for user {UserId}",
                body?.UserId);
            return Unauthorized(new { error = "Invalid or missing internal token" });
        }

        if (body is null || body.UserId <= 0 ||
            string.IsNullOrWhiteSpace(body.Type) ||
            string.IsNullOrWhiteSpace(body.Title) ||
            string.IsNullOrWhiteSpace(body.Body))
            return BadRequest(new { error = "userId, type, title and body are required" });

        // Dedup by IdempotencyKey. If the caller passed one, look it up and
        // bail out if it already exists.
        if (!string.IsNullOrWhiteSpace(body.IdempotencyKey))
        {
            var existing = await notificationRepository.FindByIdempotencyKeyAsync(body.IdempotencyKey);
            if (existing is not null)
            {
                logger.LogDebug(
                    "[internal/notifications] idempotency hit for key={Key}; returning existing id={Id}",
                    body.IdempotencyKey, existing.Id);
                return Ok(NotificationResourceFromEntityAssembler.ToResourceFromEntity(existing));
            }
        }

        try
        {
            var created = await commandService.HandleWithIdempotency(
                new CreateNotificationCommand(body.UserId, body.Type, body.Title, body.Body, body.Link),
                body.IdempotencyKey);
            return Ok(NotificationResourceFromEntityAssembler.ToResourceFromEntity(created));
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "[internal/notifications] Create failed for user {UserId} type={Type}",
                body.UserId, body.Type);
            return BadRequest(new { error = ex.Message });
        }
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

public record CreateNotificationInternalBody(
    int UserId,
    string Type,
    string Title,
    string Body,
    string? Link,
    string? IdempotencyKey);
