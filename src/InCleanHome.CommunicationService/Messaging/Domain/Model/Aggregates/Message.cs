using System.ComponentModel.DataAnnotations.Schema;
using EntityFrameworkCore.CreatedUpdatedDate.Contracts;

namespace InCleanHome.CommunicationService.Messaging.Domain.Model.Aggregates;

/// <summary>
/// Direct message between two users (local copy; Twilio is the authoritative source).
/// </summary>
public class Message : IEntityWithCreatedUpdatedDate
{
    public int Id { get; private set; }
    public int SenderId { get; private set; }
    public int RecipientId { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public DateTimeOffset? ReadAt { get; private set; }

    [Column("CreatedAt")] public DateTimeOffset? CreatedDate { get; set; }
    [Column("UpdatedAt")] public DateTimeOffset? UpdatedDate { get; set; }

    public Message() { }

    public Message(int senderId, int recipientId, string content)
    {
        SenderId    = senderId;
        RecipientId = recipientId;
        Content     = content ?? string.Empty;
        ReadAt      = null;
    }

    public Message MarkAsRead() { ReadAt ??= DateTimeOffset.UtcNow; return this; }
}
