using System.Globalization;
using ClaimsModule.Application.Common.Events;
using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Domain.Auditing;
using ClaimsModule.Domain.Events;
using MediatR;

namespace ClaimsModule.Application.Reserves.EventHandlers;

public sealed class ReserveSubmittedAuditHandler(IAuditLogService audit)
    : INotificationHandler<DomainEventNotification<ReserveSubmittedDomainEvent>>
{
    public async Task Handle(DomainEventNotification<ReserveSubmittedDomainEvent> n, CancellationToken ct)
    {
        var e = n.DomainEvent;
        var amount = e.Amount.ToString("C", CultureInfo.GetCultureInfo("en-US"));

        await audit.WriteAsync(new AuditEntry(
            e.ClaimId, AuditEventTypes.ReserveCreated,
            $"Reserve {e.Component} transaction submitted for {amount} ({e.ApprovalStatus}).",
            NewValue: e.Amount.ToString(CultureInfo.InvariantCulture),
            RelatedEntityId: e.ReserveHistoryId, RelatedEntityType: "ReserveHistory"), ct);

        if (e.AutoApproved)
            await audit.WriteAsync(new AuditEntry(
                e.ClaimId, AuditEventTypes.ReserveAutoApproved,
                $"Reserve {e.Component} transaction auto-approved for {amount}.",
                RelatedEntityId: e.ReserveHistoryId, RelatedEntityType: "ReserveHistory"), ct);
    }
}

public sealed class ReserveApprovedAuditHandler(IAuditLogService audit)
    : INotificationHandler<DomainEventNotification<ReserveApprovedDomainEvent>>
{
    public Task Handle(DomainEventNotification<ReserveApprovedDomainEvent> n, CancellationToken ct)
    {
        var e = n.DomainEvent;
        var amount = e.Amount.ToString("C", CultureInfo.GetCultureInfo("en-US"));
        return audit.WriteAsync(new AuditEntry(
            e.ClaimId, AuditEventTypes.ReserveApproved,
            $"Reserve transaction approved for {amount}.",
            RelatedEntityId: e.ReserveHistoryId, RelatedEntityType: "ReserveHistory"), ct);
    }
}

public sealed class ReserveRejectedAuditHandler(IAuditLogService audit)
    : INotificationHandler<DomainEventNotification<ReserveRejectedDomainEvent>>
{
    public Task Handle(DomainEventNotification<ReserveRejectedDomainEvent> n, CancellationToken ct)
    {
        var e = n.DomainEvent;
        return audit.WriteAsync(new AuditEntry(
            e.ClaimId, AuditEventTypes.ReserveRejected,
            $"Reserve transaction rejected: {e.RejectionReason}.",
            OldValue: e.RejectionReason,
            RelatedEntityId: e.ReserveHistoryId, RelatedEntityType: "ReserveHistory"), ct);
    }
}

public sealed class ReserveRetractedAuditHandler(IAuditLogService audit)
    : INotificationHandler<DomainEventNotification<ReserveRetractedDomainEvent>>
{
    public Task Handle(DomainEventNotification<ReserveRetractedDomainEvent> n, CancellationToken ct)
    {
        var e = n.DomainEvent;
        return audit.WriteAsync(new AuditEntry(
            e.ClaimId, AuditEventTypes.ReserveRetracted, "Reserve transaction retracted by submitter.",
            RelatedEntityId: e.ReserveHistoryId, RelatedEntityType: "ReserveHistory"), ct);
    }
}

public sealed class ReserveGlRetryRequestedAuditHandler(IAuditLogService audit)
    : INotificationHandler<DomainEventNotification<ReserveGlRetryRequestedDomainEvent>>
{
    public Task Handle(DomainEventNotification<ReserveGlRetryRequestedDomainEvent> n, CancellationToken ct)
    {
        var e = n.DomainEvent;
        return audit.WriteAsync(new AuditEntry(
            e.ClaimId, AuditEventTypes.GlPostingRetryRequested,
            "GL posting retry requested for a previously failed reserve transaction.",
            RelatedEntityId: e.ReserveHistoryId, RelatedEntityType: "ReserveHistory"), ct);
    }
}
