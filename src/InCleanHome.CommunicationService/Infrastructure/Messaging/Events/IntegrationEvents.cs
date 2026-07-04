using MassTransit;

namespace InCleanHome.CommunicationService.Infrastructure.Messaging.Events;

// From IAM Service

[MessageUrn("urn:incleanhome:event:UserRegisteredEvent")]
public record UserRegisteredEvent
{
    public int UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}

[MessageUrn("urn:incleanhome:event:WorkerDocumentsApprovedEvent")]
public record WorkerDocumentsApprovedEvent
{
    public int UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string? ApprovedBy { get; init; }
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}

[MessageUrn("urn:incleanhome:event:WorkerDocumentsRejectedEvent")]
public record WorkerDocumentsRejectedEvent
{
    public int UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string? Reason { get; init; }
    public string? RejectedBy { get; init; }
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}

[MessageUrn("urn:incleanhome:event:UserSuspendedEvent")]
public record UserSuspendedEvent
{
    public int UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public DateTimeOffset SuspendedUntil { get; init; }
    public string? SuspendedBy { get; init; }
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}

[MessageUrn("urn:incleanhome:event:UserSuspensionClearedEvent")]
public record UserSuspensionClearedEvent
{
    public int UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string? ClearedBy { get; init; }
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Published by IAM when a user registers/updates/clears their FCM device token.
/// Communication uses this to maintain a local projection (UserDevice table).
/// </summary>
[MessageUrn("urn:incleanhome:event:UserDeviceTokenUpdatedEvent")]
public record UserDeviceTokenUpdatedEvent
{
    public int UserId { get; init; }
    public string? Token { get; init; }
    public string Role { get; init; } = string.Empty;
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}

// From Booking Service 

[MessageUrn("urn:incleanhome:event:BookingCreatedEvent")]
public record BookingCreatedEvent
{
    public int BookingId { get; init; }
    public int ClientId { get; init; }
    public int WorkerId { get; init; }
    public string ClientName { get; init; } = string.Empty;
    public string WorkerName { get; init; } = string.Empty;
    public List<string> ServiceTypes { get; init; } = new();
    public DateOnly Date { get; init; }
    public string StartTime { get; init; } = string.Empty;
    public string EndTime { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}

[MessageUrn("urn:incleanhome:event:BookingConfirmedEvent")]
public record BookingConfirmedEvent
{
    public int BookingId { get; init; }
    public int ClientId { get; init; }
    public int WorkerId { get; init; }
    public string ClientName { get; init; } = string.Empty;
    public string WorkerName { get; init; } = string.Empty;
    public DateOnly Date { get; init; }
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Published by Booking Service when a client or worker reschedules a booking.
/// Mirrors the record defined in Booking Service's BookingEvents.cs.
/// </summary>
[MessageUrn("urn:incleanhome:event:BookingRescheduledEvent")]
public record BookingRescheduledEvent
{
    public int BookingId { get; init; }
    public int ClientId { get; init; }
    public int WorkerId { get; init; }
    public string ClientName { get; init; } = string.Empty;
    public string WorkerName { get; init; } = string.Empty;
    public DateOnly NewDate { get; init; }
    public string NewStartTime { get; init; } = string.Empty;
    public string NewEndTime { get; init; } = string.Empty;
    public bool RescheduledByWorker { get; init; }
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}

[MessageUrn("urn:incleanhome:event:BookingRejectedEvent")]
public record BookingRejectedEvent
{
    public int BookingId { get; init; }
    public int ClientId { get; init; }
    public int WorkerId { get; init; }
    public string ClientName { get; init; } = string.Empty;
    public string WorkerName { get; init; } = string.Empty;
    public DateOnly Date { get; init; }
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}

[MessageUrn("urn:incleanhome:event:BookingCancelledEvent")]
public record BookingCancelledEvent
{
    public int BookingId { get; init; }
    public int ClientId { get; init; }
    public int WorkerId { get; init; }
    public string ClientName { get; init; } = string.Empty;
    public string WorkerName { get; init; } = string.Empty;
    public DateOnly Date { get; init; }
    public bool CancelledByWorker { get; init; }
    public bool IsLate { get; init; }
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}

[MessageUrn("urn:incleanhome:event:BookingCompletedEvent")]
public record BookingCompletedEvent
{
    public int BookingId { get; init; }
    public int ClientId { get; init; }
    public int WorkerId { get; init; }
    public string ClientName { get; init; } = string.Empty;
    public string WorkerName { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public decimal PlatformFee { get; init; }
    public decimal WorkerEarning { get; init; }
    public int PaymentMethodId { get; init; }
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}

// ─── From Payment Service ─────────────────────────────────────────────────

[MessageUrn("urn:incleanhome:event:PaymentProcessedEvent")]
public record PaymentProcessedEvent
{
    public int PaymentId { get; init; }
    public int BookingId { get; init; }
    public int ClientId { get; init; }
    public int WorkerId { get; init; }
    public string ClientName { get; init; } = string.Empty;
    public string WorkerName { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Channel { get; init; } = string.Empty;
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}

[MessageUrn("urn:incleanhome:event:PaymentFailedEvent")]
public record PaymentFailedEvent
{
    public int BookingId { get; init; }
    public int ClientId { get; init; }
    public string Reason { get; init; } = string.Empty;
    public string Channel { get; init; } = string.Empty;
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}

// From Reviews Service 

[MessageUrn("urn:incleanhome:event:ReviewSubmittedEvent")]
public record ReviewSubmittedEvent
{
    public int ReviewId { get; init; }
    public int BookingId { get; init; }
    public int ClientId { get; init; }
    public int WorkerId { get; init; }
    public int Rating { get; init; }
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}

[MessageUrn("urn:incleanhome:event:ReportSubmittedEvent")]
public record ReportSubmittedEvent
{
    public int ReportId { get; init; }
    public int ReporterId { get; init; }
    public int ReportedUserId { get; init; }
    public string Reason { get; init; } = string.Empty;
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}

[MessageUrn("urn:incleanhome:event:ReportConfirmedEvent")]
public record ReportConfirmedEvent
{
    public int ReportId { get; init; }
    public int ReportedUserId { get; init; }
    public string? ConfirmedBy { get; init; }
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}

[MessageUrn("urn:incleanhome:event:SuspensionAppealSubmittedEvent")]
public record SuspensionAppealSubmittedEvent
{
    public int AppealId { get; init; }
    public int UserId { get; init; }
    public string Justification { get; init; } = string.Empty;
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Published by Reviews Service when an admin ACCEPTS a suspension appeal.
/// Communication sends an in-app + push notification to the user telling them
/// their suspension was lifted.
/// </summary>
[MessageUrn("urn:incleanhome:event:SuspensionAppealAcceptedEvent")]
public record SuspensionAppealAcceptedEvent
{
    public int AppealId { get; init; }
    public int UserId { get; init; }
    public string AdminResponse { get; init; } = string.Empty;
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Published by Reviews Service when an admin REJECTS a suspension appeal.
/// Communication notifies the user with the admin's response, if any.
/// </summary>
[MessageUrn("urn:incleanhome:event:SuspensionAppealRejectedEvent")]
public record SuspensionAppealRejectedEvent
{
    public int AppealId { get; init; }
    public int UserId { get; init; }
    public string AdminResponse { get; init; } = string.Empty;
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
