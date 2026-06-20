using System.Net.Mime;
using InCleanHome.CommunicationService.Infrastructure.Pipeline;
using InCleanHome.CommunicationService.Messaging.Domain.Model.Commands;
using InCleanHome.CommunicationService.Messaging.Domain.Model.Queries;
using InCleanHome.CommunicationService.Messaging.Domain.Services;
using InCleanHome.CommunicationService.Messaging.Domain.Services.External;
using InCleanHome.CommunicationService.Messaging.Interfaces.Resources;
using InCleanHome.CommunicationService.Messaging.Interfaces.Transform;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace InCleanHome.CommunicationService.Messaging.Interfaces.Controllers;

/// <summary>
/// Messaging endpoints — Twilio Conversations + legacy local fallback.
/// </summary>
[ApiController]
[Route("api/v1/messages")]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerTag("Direct messaging via Twilio Conversations")]
public class MessagesController(
    IMessageCommandService commandService,
    IMessageQueryService queryService,
    IRealtimeMessagingProvider messagingProvider) : ControllerBase
{
    // ── Twilio Conversations ──────────────────────────────────────────────

    [HttpGet("token")]
    [SwaggerOperation("Get Twilio Token", "Returns a short-lived Twilio Access Token for the current user.")]
    public IActionResult GetToken()
    {
        var current = (AuthenticatedUser?)HttpContext.Items["User"];
        if (current is null) return Unauthorized();

        var identity = $"user_{current.UserId}";
        try
        {
            var token = messagingProvider.GenerateAccessToken(identity);
            return Ok(new TwilioTokenResource(token, identity));
        }
        catch (InvalidOperationException ex) { return StatusCode(503, new { error = ex.Message }); }
    }

    [HttpPost("conversation/{userId:int}")]
    [SwaggerOperation("Get or Create Conversation", "Returns the Twilio conversation SID for the pair.")]
    public async Task<IActionResult> GetOrCreateConversation(int userId)
    {
        var current = (AuthenticatedUser?)HttpContext.Items["User"];
        if (current is null) return Unauthorized();
        if (current.UserId == userId)
            return BadRequest(new { error = "Cannot create a conversation with yourself." });

        try
        {
            var sid = await messagingProvider.GetOrCreateConversationSidAsync(
                $"user_{current.UserId}", $"user_{userId}");
            return Ok(new ConversationSidResource(sid));
        }
        catch (Exception e) { return BadRequest(new { error = e.Message }); }
    }

    // ── Legacy / local endpoints ──────────────────────────────────────────

    [HttpGet("conversations")]
    [SwaggerOperation("List Conversations (legacy)")]
    public async Task<IActionResult> ListConversations()
    {
        var current = (AuthenticatedUser?)HttpContext.Items["User"];
        if (current is null) return Unauthorized();
        var convs = await queryService.Handle(new GetConversationsForUserQuery(current.UserId));
        return Ok(convs.Select(ConversationResourceFromViewAssembler.ToResourceFromView));
    }

    [HttpGet("{userId:int}")]
    [SwaggerOperation("Get Thread (legacy)")]
    public async Task<IActionResult> GetThread(int userId)
    {
        var current = (AuthenticatedUser?)HttpContext.Items["User"];
        if (current is null) return Unauthorized();

        var messages = await queryService.Handle(new GetMessagesBetweenQuery(current.UserId, userId));
        await commandService.Handle(new MarkConversationAsReadCommand(current.UserId, userId));
        return Ok(messages.Select(MessageResourceFromEntityAssembler.ToResourceFromEntity));
    }

    [HttpPost("{userId:int}")]
    [SwaggerOperation("Send Message (legacy)")]
    public async Task<IActionResult> Send(int userId, [FromBody] SendMessageResource resource)
    {
        var current = (AuthenticatedUser?)HttpContext.Items["User"];
        if (current is null) return Unauthorized();

        try
        {
            var m = await commandService.Handle(new SendMessageCommand(current.UserId, userId, resource.Content));
            return Ok(MessageResourceFromEntityAssembler.ToResourceFromEntity(m));
        }
        catch (Exception e) { return BadRequest(new { error = e.Message }); }
    }

    [HttpPost("{userId:int}/notify")]
    [SwaggerOperation("Notify Recipient",
        "Triggers an in-app notification + push after sending a message via Twilio.")]
    public async Task<IActionResult> Notify(int userId, [FromBody] SendMessageResource resource)
    {
        var current = (AuthenticatedUser?)HttpContext.Items["User"];
        if (current is null) return Unauthorized();
        if (current.UserId == userId) return BadRequest(new { error = "Cannot notify yourself." });

        try
        {
            await commandService.Handle(new SendMessageCommand(current.UserId, userId, resource.Content ?? ""));
            return Ok(new { message = "Notification created" });
        }
        catch (Exception e) { return BadRequest(new { error = e.Message }); }
    }
}
