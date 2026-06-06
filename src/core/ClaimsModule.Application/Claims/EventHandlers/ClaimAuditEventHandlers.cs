using ClaimsModule.Application.Common.Events;
using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Domain.Auditing;
using ClaimsModule.Domain.Events;
using MediatR;

namespace ClaimsModule.Application.Claims.EventHandlers;

/// <summary>
/// Translates claim domain events into append-only audit-log entries (FRS §14.1). Handlers run
/// while the unit of work is being saved, so audit entries commit atomically with the change.
/// </summary>
public sealed class ClaimCreatedAuditHandler(IAuditLogService audit)
    : INotificationHandler<DomainEventNotification<ClaimCreatedDomainEvent>>
{
    public Task Handle(DomainEventNotification<ClaimCreatedDomainEvent> n, CancellationToken ct)
    {
        var e = n.DomainEvent;
        return audit.WriteAsync(new AuditEntry(
            e.ClaimId, AuditEventTypes.ClaimCreated,
            $"Claim {e.ClaimNumber} created.", NewValue: e.ClaimNumber), ct);
    }
}

public sealed class ClaimStatusChangedAuditHandler(IAuditLogService audit)
    : INotificationHandler<DomainEventNotification<ClaimStatusChangedDomainEvent>>
{
    public Task Handle(DomainEventNotification<ClaimStatusChangedDomainEvent> n, CancellationToken ct)
    {
        var e = n.DomainEvent;
        var reason = string.IsNullOrWhiteSpace(e.Reason) ? "" : $" ({e.Reason})";
        return audit.WriteAsync(new AuditEntry(
            e.ClaimId, AuditEventTypes.StatusChanged,
            $"Status changed from {e.From} to {e.To}{reason}.",
            OldValue: e.From.ToString(), NewValue: e.To.ToString()), ct);
    }
}

public sealed class ClaimClosedAuditHandler(IAuditLogService audit)
    : INotificationHandler<DomainEventNotification<ClaimClosedDomainEvent>>
{
    public Task Handle(DomainEventNotification<ClaimClosedDomainEvent> n, CancellationToken ct)
    {
        var e = n.DomainEvent;
        return audit.WriteAsync(new AuditEntry(
            e.ClaimId, AuditEventTypes.ClaimClosed, "Claim closed.", NewValue: e.ClosureReason), ct);
    }
}

public sealed class ClaimReopenedAuditHandler(IAuditLogService audit)
    : INotificationHandler<DomainEventNotification<ClaimReopenedDomainEvent>>
{
    public Task Handle(DomainEventNotification<ClaimReopenedDomainEvent> n, CancellationToken ct)
    {
        var e = n.DomainEvent;
        return audit.WriteAsync(new AuditEntry(
            e.ClaimId, AuditEventTypes.ClaimReopened, "Claim reopened.", NewValue: e.Reason), ct);
    }
}

public sealed class ClaimPartyAddedAuditHandler(IAuditLogService audit)
    : INotificationHandler<DomainEventNotification<ClaimPartyAddedDomainEvent>>
{
    public Task Handle(DomainEventNotification<ClaimPartyAddedDomainEvent> n, CancellationToken ct)
    {
        var e = n.DomainEvent;
        return audit.WriteAsync(new AuditEntry(
            e.ClaimId, AuditEventTypes.PartyAdded,
            $"Party added: {e.DisplayName} ({e.Role}).",
            RelatedEntityId: e.PartyId, RelatedEntityType: "ClaimParty"), ct);
    }
}

public sealed class ClaimPartyRemovedAuditHandler(IAuditLogService audit)
    : INotificationHandler<DomainEventNotification<ClaimPartyRemovedDomainEvent>>
{
    public Task Handle(DomainEventNotification<ClaimPartyRemovedDomainEvent> n, CancellationToken ct)
    {
        var e = n.DomainEvent;
        return audit.WriteAsync(new AuditEntry(
            e.ClaimId, AuditEventTypes.PartyRemoved, "Party removed.",
            RelatedEntityId: e.PartyId, RelatedEntityType: "ClaimParty"), ct);
    }
}

public sealed class ClaimDocumentUploadedAuditHandler(IAuditLogService audit)
    : INotificationHandler<DomainEventNotification<ClaimDocumentUploadedDomainEvent>>
{
    public Task Handle(DomainEventNotification<ClaimDocumentUploadedDomainEvent> n, CancellationToken ct)
    {
        var e = n.DomainEvent;
        return audit.WriteAsync(new AuditEntry(
            e.ClaimId, AuditEventTypes.DocumentUploaded,
            $"Document uploaded: {e.DocumentName}.",
            RelatedEntityId: e.DocumentId, RelatedEntityType: "ClaimDocument"), ct);
    }
}

public sealed class ClaimNotesUpdatedAuditHandler(IAuditLogService audit)
    : INotificationHandler<DomainEventNotification<ClaimNotesUpdatedDomainEvent>>
{
    public Task Handle(DomainEventNotification<ClaimNotesUpdatedDomainEvent> n, CancellationToken ct) =>
        audit.WriteAsync(new AuditEntry(
            n.DomainEvent.ClaimId, AuditEventTypes.ClaimNotesUpdated, "Claim notes updated."), ct);
}
