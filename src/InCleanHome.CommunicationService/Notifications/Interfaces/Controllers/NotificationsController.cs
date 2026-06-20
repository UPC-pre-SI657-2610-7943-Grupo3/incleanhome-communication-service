using System.Net.Mime;
using InCleanHome.CommunicationService.Infrastructure.Pipeline;
using InCleanHome.CommunicationService.Notifications.Domain.Model.Commands;
using InCleanHome.CommunicationService.Notifications.Domain.Model.Queries;
using InCleanHome.CommunicationService.Notifications.Domain.Services;
using InCleanHome.CommunicationService.Notifications.Domain.Services.External;
using InCleanHome.CommunicationService.Notifications.Interfaces.Resources;
using InCleanHome.CommunicationService.Notifications.Interfaces.Transform;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace InCleanHome.CommunicationService.Notifications.Interfaces.Controllers;

/// <summary>
/// Notification endpoints consumed by the Vue frontend.
/// </summary>
[ApiController]
[Route("api/v1/notifications")]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerTag("In-app notifications")]
public class NotificationsController(
    INotificationCommandService commandService,
    INotificationQueryService queryService,
    IPushNotificationProvider pushProvider) : ControllerBase
{
    [HttpGet]
    [SwaggerOperation("List Notifications", "Returns the current user's notifications, newest first.")]
    public async Task<IActionResult> ListMine()
    {
        var current = (AuthenticatedUser?)HttpContext.Items["User"];
        if (current is null) return Unauthorized();

        var notifications = await queryService.Handle(new GetNotificationsByUserIdQuery(current.UserId));
        return Ok(notifications.Select(NotificationResourceFromEntityAssembler.ToResourceFromEntity));
    }

    [HttpGet("unread-count")]
    [SwaggerOperation("Unread Count", "Returns the number of unread notifications.")]
    public async Task<IActionResult> UnreadCount()
    {
        var current = (AuthenticatedUser?)HttpContext.Items["User"];
        if (current is null) return Unauthorized();
        var count = await queryService.Handle(new GetUnreadCountByUserIdQuery(current.UserId));
        return Ok(new UnreadCountResource(count));
    }

    [HttpPatch("{id:int}/read")]
    [SwaggerOperation("Mark Read")]
    public async Task<IActionResult> MarkRead(int id)
    {
        var current = (AuthenticatedUser?)HttpContext.Items["User"];
        if (current is null) return Unauthorized();
        var ok = await commandService.Handle(new MarkNotificationReadCommand(id, current.UserId));
        if (!ok) return NotFound(new { error = "Notification not found" });
        return Ok(new { message = "Notification marked as read" });
    }

    [HttpPatch("read-all")]
    [SwaggerOperation("Mark All Read")]
    public async Task<IActionResult> MarkAllRead()
    {
        var current = (AuthenticatedUser?)HttpContext.Items["User"];
        if (current is null) return Unauthorized();
        await commandService.Handle(new MarkAllNotificationsReadCommand(current.UserId));
        return Ok(new { message = "All notifications marked as read" });
    }

    [HttpDelete("{id:int}")]
    [SwaggerOperation("Delete Notification")]
    public async Task<IActionResult> Delete(int id)
    {
        var current = (AuthenticatedUser?)HttpContext.Items["User"];
        if (current is null) return Unauthorized();
        var ok = await commandService.Handle(new DeleteNotificationCommand(id, current.UserId));
        if (!ok) return NotFound(new { error = "Notification not found" });
        return Ok(new { message = "Notification deleted" });
    }

    [HttpPost("test-send")]
    [SwaggerOperation("Test Firebase Push",
        "Diagnostic endpoint — sends a test push to a given FCM token.")]
    public async Task<IActionResult> TestSend([FromQuery] string token)
    {
        try
        {
            var messageId = await pushProvider.SendNotificationAsync(
                deviceToken: token,
                title: "¡Prueba de Conexión Exitosa!",
                body:  "Si lees esto, tu Communication Service y Firebase están integrados.");
            return Ok(new { success = true, messageId });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }
}
