using ClaimsModule.Application.Common.Exceptions;
using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Domain.Entities;
using ClaimsModule.Domain.Enums;
using FluentValidation;
using MediatR;

namespace ClaimsModule.Application.Reserves;

/// <summary>Rejects a pending reserve transaction (FRS §6.4, §10.2). The record is retained in
/// history and the submitter may re-submit with a revised amount (BR-R-04).</summary>
public sealed record RejectReserveCommand : IRequest<Unit>
{
    public Guid ClaimId { get; init; }
    public Guid TransactionId { get; init; }
    public string RejectionReason { get; init; } = string.Empty;
}

public sealed class RejectReserveCommandValidator : AbstractValidator<RejectReserveCommand>
{
    public RejectReserveCommandValidator() =>
        RuleFor(x => x.RejectionReason).NotEmpty().MaximumLength(500);
}

public sealed class RejectReserveCommandHandler(
    IClaimRepository claims,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork) : IRequestHandler<RejectReserveCommand, Unit>
{
    public async Task<Unit> Handle(RejectReserveCommand request, CancellationToken cancellationToken)
    {
        var claim = await claims.GetWithReservesAsync(request.ClaimId, cancellationToken)
            ?? throw new NotFoundException(nameof(Claim), request.ClaimId);

        var (component, txn) = claim.FindReserveTransaction(request.TransactionId)
            ?? throw new NotFoundException(nameof(ReserveHistory), request.TransactionId);

        if (currentUser.Role is not (UserRole.Supervisor or UserRole.Manager))
            throw new BusinessRuleException("approval",
                "Only a Supervisor or Manager may reject reserves.");

        if (txn.ApprovalStatus != ReserveApprovalStatus.PendingApproval)
            throw new BusinessRuleException("reserve", "Only a pending reserve transaction can be rejected.");

        component.Reject(txn, currentUser.UserId, request.RejectionReason);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
