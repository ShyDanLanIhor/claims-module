using ClaimsModule.Application.Common.Exceptions;
using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Domain.Entities;
using ClaimsModule.Domain.Enums;
using MediatR;

namespace ClaimsModule.Application.Reserves;

/// <summary>Submitter retracts their own pending reserve before approval (FRS §6.4, §10.2).</summary>
public sealed record RetractReserveCommand(Guid ClaimId, Guid TransactionId) : IRequest<Unit>;

public sealed class RetractReserveCommandHandler(
    IClaimRepository claims,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork) : IRequestHandler<RetractReserveCommand, Unit>
{
    public async Task<Unit> Handle(RetractReserveCommand request, CancellationToken cancellationToken)
    {
        var claim = await claims.GetWithReservesAsync(request.ClaimId, cancellationToken)
            ?? throw new NotFoundException(nameof(Claim), request.ClaimId);

        var (component, txn) = claim.FindReserveTransaction(request.TransactionId)
            ?? throw new NotFoundException(nameof(ReserveHistory), request.TransactionId);

        if (txn.SubmittedByUserId != currentUser.UserId)
            throw new ForbiddenAccessException("Only the submitter may retract a pending reserve.");

        if (txn.ApprovalStatus != ReserveApprovalStatus.PendingApproval)
            throw new BusinessRuleException("reserve", "Only a pending reserve transaction can be retracted.");

        component.Retract(txn, currentUser.UserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
