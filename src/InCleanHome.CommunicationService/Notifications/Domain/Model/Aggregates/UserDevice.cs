using System.ComponentModel.DataAnnotations.Schema;
using EntityFrameworkCore.CreatedUpdatedDate.Contracts;

namespace InCleanHome.CommunicationService.Notifications.Domain.Model.Aggregates;

/// <summary>
/// Local projection of (userId, deviceToken). Built from
/// <c>UserDeviceTokenUpdatedEvent</c> published by IAM whenever a user
/// registers or clears their FCM token. Keeping it local avoids HTTP calls
/// to IAM every time we want to send a push.
/// </summary>
public class UserDevice : IEntityWithCreatedUpdatedDate
{
    public int Id { get; private set; }
    public int UserId { get; private set; }
    public string Token { get; private set; } = string.Empty;
    public string? Role { get; private set; }

    [Column("CreatedAt")] public DateTimeOffset? CreatedDate { get; set; }
    [Column("UpdatedAt")] public DateTimeOffset? UpdatedDate { get; set; }

    public UserDevice() { }

    public UserDevice(int userId, string token, string? role)
    {
        UserId = userId;
        Token  = token;
        Role   = role;
    }

    public UserDevice UpdateToken(string token) { Token = token; return this; }
    public UserDevice UpdateRole(string role)   { Role  = role;  return this; }
}
