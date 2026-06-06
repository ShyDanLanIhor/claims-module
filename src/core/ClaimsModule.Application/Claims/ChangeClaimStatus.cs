using ClaimsModule.Application.Common.Exceptions;
using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Domain.Claims;
using ClaimsModule.Domain.Enums;
using MediatR;

namespace ClaimsModule.Application.Claims;

/// <summary>Transitions a claim's status, enforcing the state machine and entry conditions
/// (FRS §4.2–§4.3, §10.1 PUT /api/claims/{id}/status, BR-ST-01..04).</summary>
public sealed record ChangeClaimStatusCommand : IRequest<Unit>
{
    public Guid ClaimId { get; init; }
    public ClaimStatus TargetStatus { get; init; }
    public string? Reason { get; init; }

    /// <summary>Set when the caller has acknowledged the non-blocking policy-period warning (BR-C-02)
    /// so a claim whose loss date is outside the policy period may still transition Draft→Open.</summary>
    public bool AcknowledgeWarnings { get; init; }
}

public sealed class ChangeClaimStatusCommandHandler(
    IClaimRepository claims,
    IPolicyRepository policies,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork) : IRequestHandler<ChangeClaimStatusCommand, Unit>
{
    public async Task<Unit> Handle(ChangeClaimStatusCommand request, CancellationToken cancellationToken)
    {
        var claim = await claims.GetAggregateAsync(request.ClaimId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Claim), request.ClaimId);

        var from = claim.Status;
        var to = request.TargetStatus;

        if (!ClaimLifecycle.IsAllowed(from, to))
        {
            var allowed = string.Join(", ", ClaimLifecycle.AllowedNext(from));
            throw new BusinessRuleException("targetStatus",
                $"Transition from {from} to {to} is not permitted. Valid next statuses: [{allowed}]."); // BR-ST-01
        }

        EnsureRole(from, to);

        // BR-C-02: the policy-period warning must be acknowledged before Draft→Open. It is re-derived here
        // (loss date vs policy effective period) rather than read from a validation-issue store, which the
        // FRS entity model does not define. See ARCHITECTURE.md §10.
        if (to == ClaimStatus.Open && !request.AcknowledgeWarnings
            && await HasPolicyPeriodWarningAsync(claim, cancellationToken))
            throw new BusinessRuleException("acknowledgeWarnings",
                "Loss date is outside the policy effective period — acknowledge this warning to open the claim.");

        switch (to)
        {
            case ClaimStatus.Open when !claim.HasActiveClaimant:
                throw new BusinessRuleException("claimParties",
                    "At least one Claimant party is required to open a claim."); // BR-ST-02

            case ClaimStatus.PendingPayment when !HasApprovedReserve(claim):
                throw new BusinessRuleException("reserves",
                    "At least one approved reserve component is required before payment processing.");

            case ClaimStatus.Withdrawn when string.IsNullOrWhiteSpace(request.Reason):
                throw new BusinessRuleException("reason", "A withdrawal reason is required.");

            case ClaimStatus.Reopened when string.IsNullOrWhiteSpace(request.Reason):
                throw new BusinessRuleException("reason", "A reopen reason is required."); // BR-ST-04

            case ClaimStatus.Closed:
                EnsureClosable(claim, request.Reason); // §4.3 CC-01..04
                break;
        }

        if (to == ClaimStatus.Reopened)
            claim.Reopen(request.Reason!); // Closed → Reopened → Open in one move
        else
            claim.ChangeStatus(to, request.Reason);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }

    private void EnsureRole(ClaimStatus from, ClaimStatus to)
    {
        var required = ClaimLifecycle.RequiredRole(from, to);
        if (required is { } role && (int)currentUser.Role < (int)role)
            throw new ForbiddenAccessException($"This transition requires the {role} role or higher.");
    }

    private async Task<bool> HasPolicyPeriodWarningAsync(Domain.Entities.Claim claim, CancellationToken cancellationToken)
    {
        if (claim.PolicyId is not { } policyId || claim.LossEvent is null)
            return false;

        var policy = await policies.GetByIdAsync(policyId, cancellationToken);
        if (policy is null)
            return false;

        var lossDate = DateOnly.FromDateTime(claim.LossEvent.LossDate.UtcDateTime);
        return !policy.CoversDate(lossDate);
    }

    private static bool HasApprovedReserve(Domain.Entities.Claim claim) =>
        claim.ReserveComponents.Any(c => c.History.Any(h => h.IsEffective));

    private static void EnsureClosable(Domain.Entities.Claim claim, string? justification)
    {
        var blockers = new List<string>();

        if (claim.ReserveComponents.Any(c => c.History.Any(h =>
                h.ApprovalStatus == ReserveApprovalStatus.PendingApproval)))
            blockers.Add("No reserve components may remain in PendingApproval."); // CC-01

        // CC-02: unresolved Critical validation issues are enforced preventively — they are blocked at
        // claim creation (FluentValidation) and at the Draft → Open gate (BR-ST-02), so a claim cannot
        // reach a closable status while carrying one. There is no separate validation-issue store (the
        // FRS entity model defines none); see ARCHITECTURE.md §10.

        if (!claim.HasActiveClaimant)
            blockers.Add("At least one Claimant party is required."); // CC-03

        // CC-04: if any single component carries a positive balance, an explicit justification is required.
        if (claim.HasOpenReserve && string.IsNullOrWhiteSpace(justification))
            blockers.Add("Open reserves exist — a justification note is required to close the claim.");

        if (blockers.Count != 0)
            throw new BusinessRuleException(new Dictionary<string, string[]> { ["closure"] = [.. blockers] });
    }
}
