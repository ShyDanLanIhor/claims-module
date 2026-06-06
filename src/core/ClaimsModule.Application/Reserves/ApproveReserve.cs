using ClaimsModule.Application.Common.Exceptions;
using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Domain.Entities;
using ClaimsModule.Domain.Enums;
using ClaimsModule.Domain.Reserves;
using MediatR;

namespace ClaimsModule.Application.Reserves;

/// <summary>Approves a pending reserve transaction (FRS §6.4, §10.2). Requires Supervisor/Manager
/// authority for the amount, forbids self-approval, and enqueues GL posting on success.</summary>
public sealed record ApproveReserveCommand : IRequest<Unit>
{
    public Guid ClaimId { get; init; }
    public Guid TransactionId { get; init; }

    /// <summary>A Manager may set this to authorise crossing the $10M aggregate limit (BR-R-05).</summary>
    public bool ApplyManagerOverride { get; init; }
}

public sealed class ApproveReserveCommandHandler(
    IClaimRepository claims,
    IBackgroundJobScheduler jobs,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork) : IRequestHandler<ApproveReserveCommand, Unit>
{
    public async Task<Unit> Handle(ApproveReserveCommand request, CancellationToken cancellationToken)
    {
        var claim = await claims.GetWithReservesAsync(request.ClaimId, cancellationToken)
            ?? throw new NotFoundException(nameof(Claim), request.ClaimId);

        var (component, txn) = claim.FindReserveTransaction(request.TransactionId)
            ?? throw new NotFoundException(nameof(ReserveHistory), request.TransactionId);

        if (txn.ApprovalStatus != ReserveApprovalStatus.PendingApproval)
            throw new BusinessRuleException("reserve", "Only a pending reserve transaction can be approved.");

        if (currentUser.Role is not (UserRole.Supervisor or UserRole.Manager)
            || !ReserveAuthority.CanApprove(currentUser.Role, txn.Amount))
            throw new BusinessRuleException("approval",
                "Your role does not have authority to approve this reserve amount."); // BR-R-02

        if (txn.SubmittedByUserId == currentUser.UserId)
            throw new BusinessRuleException("approval", "Self-approval is not permitted."); // BR-R-03

        // BR-R-05: crossing the aggregate limit needs a manager override.
        ReserveOverride.Ensure(claim, txn.Amount, currentUser, request.ApplyManagerOverride);

        component.Approve(txn, currentUser.UserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await ReserveGlPosting.ScheduleAsync(jobs, unitOfWork, component, txn, cancellationToken); // FRS §6.5
        return Unit.Value;
    }
}
