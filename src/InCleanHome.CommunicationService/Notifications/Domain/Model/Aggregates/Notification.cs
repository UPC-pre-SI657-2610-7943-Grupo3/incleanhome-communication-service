using System.ComponentModel.DataAnnotations.Schema;
using EntityFrameworkCore.CreatedUpdatedDate.Contracts;

namespace InCleanHome.CommunicationService.Notifications.Domain.Model.Aggregates;

/// <summary>
/// Notification aggregate root — an in-app message addressed to one user.
/// </summary>
public class Notification : IEntityWithCreatedUpdatedDate
{
    public int Id { get; private set; }
    public int UserId { get; private set; }
    public string Type { get; private set; } = "info";
    public string Title { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public string? Link { get; private set; }
    public bool Read { get; private set; }

    /// <summary>
    ///   Optional idempotency key used when this notification was created via
    ///   the internal HTTP endpoint. Lets other microservices safely retry
    ///   without producing duplicates if both the broker delivery and the
    ///   HTTP call succeed for the same domain event.
    /// </summary>
    public string? IdempotencyKey { get; private set; }

    [Column("CreatedAt")] public DateTimeOffset? CreatedDate { get; set; }
    [Column("UpdatedAt")] public DateTimeOffset? UpdatedDate { get; set; }

    public Notification() { }

    public Notification(int userId, string type, string title, string body, string? link)
    {
        UserId = userId;
        Type   = string.IsNullOrWhiteSpace(type) ? "info" : type;
        Title  = title;
        Body   = body;
        Link   = link;
        Read   = false;
    }

    public Notification(int userId, string type, string title, string body, string? link, string? idempotencyKey)
        : this(userId, type, title, body, link)
    {
        IdempotencyKey = idempotencyKey;
    }

    public Notification MarkAsRead() { Read = true; return this; }
}
