using InCleanHome.CommunicationService.Infrastructure.Messaging.Events;
using InCleanHome.CommunicationService.Notifications.Domain.Model.Commands;
using InCleanHome.CommunicationService.Notifications.Domain.Services;
using MassTransit;

namespace InCleanHome.CommunicationService.Infrastructure.Messaging.Consumers;


// Each consumer translates an integration event into 1+ in-app         
// notifications by calling INotificationCommandService.                
// Naming convention: <EventName>Consumer.                               
// Failure handling: NotificationCommandService is best-effort by      
// design (DB save + push wrapped). If it throws, MassTransit retries   
// and eventually moves the message to the error queue.                 

// IAM events 

public class UserRegisteredConsumer(
    INotificationCommandService notifications) : IConsumer<UserRegisteredEvent>
{
    public async Task Consume(ConsumeContext<UserRegisteredEvent> ctx)
    {
        var e = ctx.Message;
        await notifications.Handle(new CreateNotificationCommand(
            UserId: e.UserId, Type: "welcome",
            Title:  "¡Bienvenido(a) a InCleanHome!",
            Body:   "Tu cuenta se ha creado correctamente. Empieza a usar la plataforma.",
            Link:   e.Role == "worker" ? "/worker/dashboard" : "/client/dashboard"));
    }
}

public class WorkerDocumentsApprovedConsumer(
    INotificationCommandService notifications) : IConsumer<WorkerDocumentsApprovedEvent>
{
    public async Task Consume(ConsumeContext<WorkerDocumentsApprovedEvent> ctx)
    {
        var e = ctx.Message;
        await notifications.Handle(new CreateNotificationCommand(
            UserId: e.UserId, Type: "documents_approved",
            Title:  "Documentos aprobados",
            Body:   "Tus documentos fueron verificados. Ya puedes recibir reservas.",
            Link:   "/worker/dashboard"));
    }
}

public class WorkerDocumentsRejectedConsumer(
    INotificationCommandService notifications) : IConsumer<WorkerDocumentsRejectedEvent>
{
    public async Task Consume(ConsumeContext<WorkerDocumentsRejectedEvent> ctx)
    {
        var e = ctx.Message;
        var body = string.IsNullOrWhiteSpace(e.Reason)
            ? "Por favor revisa tus documentos y vuelve a subirlos."
            : $"Motivo: {e.Reason}. Por favor vuelve a subirlos.";
        await notifications.Handle(new CreateNotificationCommand(
            UserId: e.UserId, Type: "documents_rejected",
            Title:  "Documentos rechazados",
            Body:   body,
            Link:   "/worker/profile"));
    }
}

public class UserSuspendedConsumer(
    INotificationCommandService notifications) : IConsumer<UserSuspendedEvent>
{
    public async Task Consume(ConsumeContext<UserSuspendedEvent> ctx)
    {
        var e = ctx.Message;
        await notifications.Handle(new CreateNotificationCommand(
            UserId: e.UserId, Type: "suspended",
            Title:  "Cuenta suspendida temporalmente",
            Body:   $"Motivo: {e.Reason}. Suspensión hasta {e.SuspendedUntil:yyyy-MM-dd HH:mm} UTC. " +
                    "Puedes apelar desde tu perfil.",
            Link:   "/suspension-appeal"));
    }
}

public class UserSuspensionClearedConsumer(
    INotificationCommandService notifications) : IConsumer<UserSuspensionClearedEvent>
{
    public async Task Consume(ConsumeContext<UserSuspensionClearedEvent> ctx)
    {
        var e = ctx.Message;
        await notifications.Handle(new CreateNotificationCommand(
            UserId: e.UserId, Type: "suspension_cleared",
            Title:  "Suspensión levantada",
            Body:   "Tu cuenta vuelve a estar activa. Ya puedes usar la plataforma normalmente.",
            Link:   "/"));
    }
}

// Booking events

public class BookingCreatedConsumer(
    INotificationCommandService notifications) : IConsumer<BookingCreatedEvent>
{
    public async Task Consume(ConsumeContext<BookingCreatedEvent> ctx)
    {
        var e = ctx.Message;
        await notifications.Handle(new CreateNotificationCommand(
            UserId: e.WorkerId, Type: "booking_new",
            Title:  "Nueva solicitud de servicio",
            Body:   $"{e.ClientName} solicitó tu servicio para el {e.Date:yyyy-MM-dd} de {e.StartTime} a {e.EndTime}.",
            Link:   "/worker/requests"));
    }
}

public class BookingConfirmedConsumer(
    INotificationCommandService notifications) : IConsumer<BookingConfirmedEvent>
{
    public async Task Consume(ConsumeContext<BookingConfirmedEvent> ctx)
    {
        var e = ctx.Message;
        await notifications.Handle(new CreateNotificationCommand(
            UserId: e.ClientId, Type: "booking_confirmed",
            Title:  "Tu servicio fue confirmado",
            Body:   $"{e.WorkerName} aceptó tu reserva del {e.Date:yyyy-MM-dd}.",
            Link:   "/client/bookings"));
    }
}

/// <summary>
/// Notifica a la contraparte cuando una reserva es reprogramada (por cliente o
/// trabajadora). Existía en el monolito (vía INotificationsContextFacade) y se
/// había perdido en la migración a microservicios: Booking Service ya publica
/// BookingRescheduledEvent pero nadie lo consumía.
/// </summary>
public class BookingRescheduledConsumer(
    INotificationCommandService notifications) : IConsumer<BookingRescheduledEvent>
{
    public async Task Consume(ConsumeContext<BookingRescheduledEvent> ctx)
    {
        var e = ctx.Message;
        if (e.RescheduledByWorker)
        {
            await notifications.Handle(new CreateNotificationCommand(
                UserId: e.ClientId, Type: "booking_rescheduled",
                Title:  "Reserva reprogramada",
                Body:   $"La trabajador(a) {e.WorkerName} reprogramó tu reserva al {e.NewDate:yyyy-MM-dd} ({e.NewStartTime}–{e.NewEndTime}).",
                Link:   "/client/bookings"));
        }
        else
        {
            await notifications.Handle(new CreateNotificationCommand(
                UserId: e.WorkerId, Type: "booking_rescheduled",
                Title:  "Solicitud reprogramada",
                Body:   $"{e.ClientName} reprogramó la reserva al {e.NewDate:yyyy-MM-dd} ({e.NewStartTime}–{e.NewEndTime}). Necesita tu confirmación.",
                Link:   "/worker/requests"));
        }
    }
}

public class BookingRejectedConsumer(
    INotificationCommandService notifications) : IConsumer<BookingRejectedEvent>
{
    public async Task Consume(ConsumeContext<BookingRejectedEvent> ctx)
    {
        var e = ctx.Message;
        await notifications.Handle(new CreateNotificationCommand(
            UserId: e.ClientId, Type: "booking_rejected",
            Title:  "Tu servicio fue rechazado",
            Body:   $"{e.WorkerName} rechazó tu solicitud del {e.Date:yyyy-MM-dd}. Puedes buscar otra trabajadora.",
            Link:   "/client/search"));
    }
}

public class BookingCancelledConsumer(
    INotificationCommandService notifications) : IConsumer<BookingCancelledEvent>
{
    public async Task Consume(ConsumeContext<BookingCancelledEvent> ctx)
    {
        var e = ctx.Message;
        // Notify the OTHER party.
        var recipientId = e.CancelledByWorker ? e.ClientId : e.WorkerId;
        var canceller   = e.CancelledByWorker ? e.WorkerName : e.ClientName;
        var lateNote    = e.IsLate ? " Se ha aplicado una sanción por cancelación tardía." : string.Empty;
        await notifications.Handle(new CreateNotificationCommand(
            UserId: recipientId, Type: "booking_cancelled",
            Title:  "Reserva cancelada",
            Body:   $"{canceller} canceló la reserva del {e.Date:yyyy-MM-dd}.{lateNote}",
            Link:   e.CancelledByWorker ? "/client/bookings" : "/worker/requests"));
    }
}

public class BookingCompletedConsumer(
    INotificationCommandService notifications) : IConsumer<BookingCompletedEvent>
{
    public async Task Consume(ConsumeContext<BookingCompletedEvent> ctx)
    {
        var e = ctx.Message;
        // Notify the client to pay + leave a review.
        await notifications.Handle(new CreateNotificationCommand(
            UserId: e.ClientId, Type: "booking_completed",
            Title:  "Servicio completado",
            Body:   $"{e.WorkerName} marcó el servicio como completado. Recuerda pagar y dejar tu reseña.",
            Link:   "/client/bookings"));
    }
}

// Payment events

public class PaymentProcessedConsumer(
    INotificationCommandService notifications) : IConsumer<PaymentProcessedEvent>
{
    public async Task Consume(ConsumeContext<PaymentProcessedEvent> ctx)
    {
        var e = ctx.Message;
        // Notify the worker that the service was paid.
        await notifications.Handle(new CreateNotificationCommand(
            UserId: e.WorkerId, Type: "payment",
            Title:  "Servicio pagado",
            Body:   $"{e.ClientName} pagó tu servicio. Revisa el detalle en tus solicitudes completadas.",
            Link:   "/worker/requests"));
    }
}

public class PaymentFailedConsumer(
    INotificationCommandService notifications) : IConsumer<PaymentFailedEvent>
{
    public async Task Consume(ConsumeContext<PaymentFailedEvent> ctx)
    {
        var e = ctx.Message;
        await notifications.Handle(new CreateNotificationCommand(
            UserId: e.ClientId, Type: "payment_failed",
            Title:  "El pago no se pudo procesar",
            Body:   $"Motivo: {e.Reason}. Inténtalo nuevamente desde tus reservas.",
            Link:   "/client/bookings"));
    }
}

// Reviews events 

public class ReviewSubmittedConsumer(
    INotificationCommandService notifications) : IConsumer<ReviewSubmittedEvent>
{
    public async Task Consume(ConsumeContext<ReviewSubmittedEvent> ctx)
    {
        var e = ctx.Message;
        await notifications.Handle(new CreateNotificationCommand(
            UserId: e.WorkerId, Type: "review",
            Title:  "Nueva reseña recibida",
            Body:   $"Recibiste una reseña de {e.Rating} estrella(s). ¡Sigue así!",
            Link:   "/worker/profile"));
    }
}

public class ReportSubmittedConsumer(
    INotificationCommandService notifications) : IConsumer<ReportSubmittedEvent>
{
    public async Task Consume(ConsumeContext<ReportSubmittedEvent> ctx)
    {
        var e = ctx.Message;
        // Inform the reporter their report was received.
        await notifications.Handle(new CreateNotificationCommand(
            UserId: e.ReporterId, Type: "report_submitted",
            Title:  "Tu reporte fue recibido",
            Body:   "Vamos a revisarlo y te avisaremos del resultado.",
            Link:   null));
    }
}

public class ReportConfirmedConsumer(
    INotificationCommandService notifications) : IConsumer<ReportConfirmedEvent>
{
    public async Task Consume(ConsumeContext<ReportConfirmedEvent> ctx)
    {
        var e = ctx.Message;
        // Inform the reported user that the report was confirmed.
        await notifications.Handle(new CreateNotificationCommand(
            UserId: e.ReportedUserId, Type: "report_confirmed",
            Title:  "Reporte confirmado",
            Body:   "Un reporte sobre tu cuenta fue confirmado por administración. Revisa tu correo.",
            Link:   null));
    }
}

public class SuspensionAppealSubmittedConsumer(
    INotificationCommandService notifications) : IConsumer<SuspensionAppealSubmittedEvent>
{
    public async Task Consume(ConsumeContext<SuspensionAppealSubmittedEvent> ctx)
    {
        var e = ctx.Message;
        await notifications.Handle(new CreateNotificationCommand(
            UserId: e.UserId, Type: "appeal_submitted",
            Title:  "Apelación enviada",
            Body:   "Tu apelación fue recibida. Vamos a revisarla.",
            Link:   "/suspension-appeal"));
    }
}

public class SuspensionAppealAcceptedConsumer(
    INotificationCommandService notifications) : IConsumer<SuspensionAppealAcceptedEvent>
{
    public async Task Consume(ConsumeContext<SuspensionAppealAcceptedEvent> ctx)
    {
        var e = ctx.Message;
        var extra = string.IsNullOrWhiteSpace(e.AdminResponse)
            ? string.Empty
            : $" Mensaje del equipo: \"{e.AdminResponse}\"";

        await notifications.Handle(new CreateNotificationCommand(
            UserId: e.UserId, Type: "appeal_accepted",
            Title:  "Tu reclamo fue aceptado",
            Body:   $"Tu suspensión ha sido levantada. Ya puedes usar la plataforma normalmente.{extra}",
            Link:   "/"));
    }
}

public class SuspensionAppealRejectedConsumer(
    INotificationCommandService notifications) : IConsumer<SuspensionAppealRejectedEvent>
{
    public async Task Consume(ConsumeContext<SuspensionAppealRejectedEvent> ctx)
    {
        var e = ctx.Message;
        var extra = string.IsNullOrWhiteSpace(e.AdminResponse)
            ? string.Empty
            : $" Motivo: \"{e.AdminResponse}\"";

        await notifications.Handle(new CreateNotificationCommand(
            UserId: e.UserId, Type: "appeal_rejected",
            Title:  "Tu reclamo fue rechazado",
            Body:   $"Tu suspensión continúa.{extra}",
            Link:   "/suspension-appeal"));
    }
}
