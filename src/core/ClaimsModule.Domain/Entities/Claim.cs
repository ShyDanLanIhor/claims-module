using ClaimsModule.Domain.Common;
using ClaimsModule.Domain.Enums;
using ClaimsModule.Domain.Events;
using ClaimsModule.Domain.Reserves;

namespace ClaimsModule.Domain.Entities;

/// <summary>
/// The claim aggregate root (FRS §9.1). Owns its loss event, parties, risk objects, reserve
/// components and documents, and is the boundary for status-lifecycle behaviour. Status changes,
/// closure and reopening flow through methods here so domain events are raised consistently;
/// the cross-entity preconditions that need a structured 422 (closure conditions, role checks)
/// are validated in the Application layer before these methods are invoked.
/// </summary>
public class Claim : AuditableEntity, IAggregateRoot
{
    private readonly List<ClaimParty> _parties = [];
    private readonly List<ClaimRiskObject> _riskObjects = [];
    private readonly List<ClaimReserveComponent> _reserveComponents = [];
    private readonly List<ClaimDocument> _documents = [];

    public string ClaimNumber { get; set; } = string.Empty;
    public Guid? PolicyId { get; set; }
    public string? PolicyNumber { get; set; }
    public string? ClientName { get; set; }
    public ClaimStatus Status { get; private set; } = ClaimStatus.Draft;
    public ClaimSeverity? Severity { get; set; }
    public DateTimeOffset ReportedDate { get; set; }
    public Guid? AssignedHandlerId { get; set; }
    public DateTimeOffset? ClosedAt { get; private set; }
    public string? ClosureReason { get; private set; }
    public string? Notes { get; private set; }

    /// <summary>Set by a Manager to permit aggregate reserves above the $10M limit (BR-R-05).</summary>
    public bool ManagerOverrideApplied { get; private set; }

    public byte[] RowVersion { get; set; } = [];

    public LossEvent LossEvent { get; set; } = null!;
    public IReadOnlyCollection<ClaimParty> Parties => _parties.AsReadOnly();
    public IReadOnlyCollection<ClaimRiskObject> RiskObjects => _riskObjects.AsReadOnly();
    public IReadOnlyCollection<ClaimReserveComponent> ReserveComponents => _reserveComponents.AsReadOnly();
    public IReadOnlyCollection<ClaimDocument> Documents => _documents.AsReadOnly();

    private Claim() { }

    /// <summary>Creates a new claim in Draft with its loss event attached (FNOL intake, FRS §5).</summary>
    public static Claim Create(
        Guid organisationId,
        string claimNumber,
        Guid? policyId,
        string? policyNumber,
        string? clientName,
        DateTimeOffset reportedDate,
        Guid? assignedHandlerId,
        ClaimSeverity? severity,
        string? notes,
        LossEvent lossEvent)
    {
        var claim = new Claim
        {
            OrganisationId = organisationId,
            ClaimNumber = claimNumber,
            PolicyId = policyId,
            PolicyNumber = policyNumber,
            ClientName = clientName,
            Status = ClaimStatus.Draft,
            Severity = severity,
            ReportedDate = reportedDate,
            AssignedHandlerId = assignedHandlerId,
            Notes = notes
        };

        lossEvent.ClaimId = claim.Id;
        lossEvent.OrganisationId = organisationId;
        claim.LossEvent = lossEvent;

        claim.RaiseDomainEvent(new ClaimCreatedDomainEvent(claim.Id, claimNumber));
        return claim;
    }

    public bool HasActiveClaimant =>
        _parties.Any(p => p.IsActive && p.PartyRole == PartyRole.Claimant);

    public decimal TotalApprovedReserves => _reserveComponents.Sum(c => c.CurrentAmount);

    /// <summary>BR-R-05: true if making <paramref name="pendingAmount"/> effective would push total
    /// approved reserves past the aggregate limit while no manager override is in force. The rule lives
    /// here, next to the state it governs, so every caller evaluates it the same way.</summary>
    public bool WouldExceedAggregateLimit(decimal pendingAmount) =>
        !ManagerOverrideApplied &&
        TotalApprovedReserves + pendingAmount > ReserveAuthority.AggregateOverrideLimit;

    /// <summary>True if any individual component carries a positive balance (closure check CC-04).
    /// Uses a per-component test, not the aggregate sum, so a negative subrogation reserve cannot mask
    /// an open positive reserve.</summary>
    public bool HasOpenReserve => _reserveComponents.Any(c => c.CurrentAmount > 0);

    public ClaimParty AddParty(ClaimParty party)
    {
        party.ClaimId = Id;
        party.OrganisationId = OrganisationId;
        _parties.Add(party);
        RaiseDomainEvent(new ClaimPartyAddedDomainEvent(Id, party.Id, party.PartyRole, party.DisplayName));
        return party;
    }

    public void RemoveParty(ClaimParty party)
    {
        if (!_parties.Contains(party))
            throw new DomainException("Party does not belong to this claim.");

        party.IsActive = false;
        RaiseDomainEvent(new ClaimPartyRemovedDomainEvent(Id, party.Id));
    }

    public ClaimRiskObject AddRiskObject(ClaimRiskObject riskObject)
    {
        riskObject.ClaimId = Id;
        riskObject.OrganisationId = OrganisationId;
        _riskObjects.Add(riskObject);
        return riskObject;
    }

    public ClaimReserveComponent AddReserveComponent(ClaimReserveComponent component)
    {
        if (component.ClaimId != Id)
            throw new DomainException("Reserve component does not belong to this claim.");

        _reserveComponents.Add(component);
        return component;
    }

    public ClaimDocument AddDocument(ClaimDocument document)
    {
        document.ClaimId = Id;
        document.OrganisationId = OrganisationId;
        _documents.Add(document);
        RaiseDomainEvent(new ClaimDocumentUploadedDomainEvent(Id, document.Id, document.DocumentName));
        return document;
    }

    /// <summary>
    /// Applies a validated status transition. Allowed-transition, role and closure-condition checks
    /// are performed by the Application layer; this method assumes they passed and is the defensive
    /// last line of defence.
    /// </summary>
    public void ChangeStatus(ClaimStatus target, string? reason)
    {
        if (!Claims.ClaimLifecycle.IsAllowed(Status, target))
            throw new DomainException($"Transition from {Status} to {target} is not permitted.");

        var previous = Status;
        Status = target;

        if (target == ClaimStatus.Closed)
        {
            ClosedAt = DateTimeOffset.UtcNow;
            ClosureReason = reason;
            RaiseDomainEvent(new ClaimClosedDomainEvent(Id, reason));
        }

        RaiseDomainEvent(new ClaimStatusChangedDomainEvent(Id, previous, target, reason));
    }

    /// <summary>Reopens a closed claim and immediately moves it to Open (FRS §4.2, BR-ST-04).</summary>
    public void Reopen(string reason)
    {
        if (Status != ClaimStatus.Closed)
            throw new DomainException("Only a closed claim can be reopened.");

        ClosedAt = null;
        ClosureReason = null;
        Status = ClaimStatus.Open;

        RaiseDomainEvent(new ClaimReopenedDomainEvent(Id, reason));
        RaiseDomainEvent(new ClaimStatusChangedDomainEvent(Id, ClaimStatus.Closed, ClaimStatus.Open, reason));
    }

    public void ApplyManagerOverride() => ManagerOverrideApplied = true;

    /// <summary>Updates the claim's free-text notes (FRS §11.3 editable notes field).</summary>
    public void UpdateNotes(string? notes)
    {
        Notes = notes;
        RaiseDomainEvent(new ClaimNotesUpdatedDomainEvent(Id));
    }

    /// <summary>Locates a reserve transaction and its owning component across all components.</summary>
    public (ClaimReserveComponent Component, ReserveHistory Transaction)? FindReserveTransaction(Guid transactionId)
    {
        foreach (var component in _reserveComponents)
        {
            var txn = component.History.FirstOrDefault(h => h.Id == transactionId);
            if (txn is not null)
                return (component, txn);
        }

        return null;
    }
}
