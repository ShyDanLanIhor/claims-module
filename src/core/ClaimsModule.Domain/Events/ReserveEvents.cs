using ClaimsModule.Domain.Common;
using ClaimsModule.Domain.Enums;

namespace ClaimsModule.Domain.Events;

public sealed record ReserveSubmittedDomainEvent(
    Guid ClaimId,
    Guid ReserveComponentId,
    Guid ReserveHistoryId,
    ReserveComponentType Component,
    decimal Amount,
    ReserveApprovalStatus ApprovalStatus,
    bool AutoApproved) : DomainEventBase;

public sealed record ReserveApprovedDomainEvent(
    Guid ClaimId, Guid ReserveHistoryId, decimal Amount, Guid ApprovedByUserId) : DomainEventBase;

public sealed record ReserveRejectedDomainEvent(
    Guid ClaimId, Guid ReserveHistoryId, Guid RejectedByUserId, string RejectionReason) : DomainEventBase;

public sealed record ReserveRetractedDomainEvent(
    Guid ClaimId, Guid ReserveHistoryId, Guid RetractedByUserId) : DomainEventBase;

public sealed record ReserveGlRetryRequestedDomainEvent(
    Guid ClaimId, Guid ReserveHistoryId, Guid RequestedByUserId) : DomainEventBase;
