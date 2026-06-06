using ClaimsModule.Application.Common.Exceptions;
using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Domain.Entities;
using ClaimsModule.Domain.Enums;
using MediatR;

namespace ClaimsModule.Application.Reserves;

/// <summary>
/// Re-enqueues GL posting for a reserve transaction whose posting previously failed (FRS §11.3 /
/// §12.1). This is an operational mitigation, not a financial change — the transaction is already
/// effective and its balance is unaffected — so it requires Supervisor/Manager authority. The
/// posting job is idempotent, so a retry can never double-post.
/// </summary>
public sealed record RetryGlPostingCommand(Guid ClaimId, Guid TransactionId) : IRequest<Unit>;

public sealed class RetryGlPostingCommandHandler(
    IClaimRepository claims,
    IBackgroundJobScheduler jobs,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork) : IRequestHandler<RetryGlPostingCommand, Unit>
{
    public async Task<Unit> Handle(RetryGlPostingCommand request, CancellationToken cancellationToken)
    {
        var claim = await claims.GetWithReservesAsync(request.ClaimId, cancellationToken)
            ?? throw new NotFoundException(nameof(Claim), request.ClaimId);

        var (component, txn) = claim.FindReserveTransaction(request.TransactionId)
            ?? throw new NotFoundException(nameof(ReserveHistory), request.TransactionId);

        if (currentUser.Role is not (UserRole.Supervisor or UserRole.Manager))
            throw new BusinessRuleException("retry",
                "Only a Supervisor or Manager can retry GL posting."); // operational authority

        if (txn.PostingStatus != PostingStatus.Failed)
            throw new BusinessRuleException("retry",
                "Only a failed GL posting can be retried.");

        component.RetryGlPosting(txn, currentUser.UserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await ReserveGlPosting.ScheduleAsync(jobs, unitOfWork, component, txn, cancellationToken); // FRS §6.5
        return Unit.Value;
    }
}
