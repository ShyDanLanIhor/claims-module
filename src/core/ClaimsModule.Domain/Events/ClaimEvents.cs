using ClaimsModule.Domain.Common;
using ClaimsModule.Domain.Enums;

namespace ClaimsModule.Domain.Events;

public sealed record ClaimCreatedDomainEvent(Guid ClaimId, string ClaimNumber) : DomainEventBase;

public sealed record ClaimStatusChangedDomainEvent(
    Guid ClaimId, ClaimStatus From, ClaimStatus To, string? Reason) : DomainEventBase;

public sealed record ClaimClosedDomainEvent(Guid ClaimId, string? ClosureReason) : DomainEventBase;

public sealed record ClaimReopenedDomainEvent(Guid ClaimId, string Reason) : DomainEventBase;

public sealed record ClaimPartyAddedDomainEvent(
    Guid ClaimId, Guid PartyId, PartyRole Role, string DisplayName) : DomainEventBase;

public sealed record ClaimPartyRemovedDomainEvent(Guid ClaimId, Guid PartyId) : DomainEventBase;

public sealed record ClaimDocumentUploadedDomainEvent(
    Guid ClaimId, Guid DocumentId, string DocumentName) : DomainEventBase;

public sealed record ClaimNotesUpdatedDomainEvent(Guid ClaimId) : DomainEventBase;
