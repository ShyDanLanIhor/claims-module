using ClaimsModule.Application.Common.Exceptions;
using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Domain.Entities;
using ClaimsModule.Domain.Enums;
using ClaimsModule.Domain.Reserves;
using FluentValidation;
using MediatR;

namespace ClaimsModule.Application.Reserves;

/// <summary>Opens or adjusts a reserve component (FRS §6.3, §10.2 POST /api/claims/{id}/reserves).
/// Amounts ≤ $10k auto-approve and enqueue GL posting; larger amounts are created PendingApproval.</summary>
public sealed record SubmitReserveCommand : IRequest<SubmitReserveResult>
{
    public Guid ClaimId { get; init; }
    public ReserveComponentType Component { get; init; }
    public decimal Amount { get; init; }
    public string ChangeReason { get; init; } = string.Empty;
    public ReserveTransactionType TransactionType { get; init; } = ReserveTransactionType.Add;

    /// <summary>A Manager may set this to authorise an auto-approved reserve that crosses the $10M
    /// aggregate limit in a single call (BR-R-05).</summary>
    public bool ApplyManagerOverride { get; init; }
}

public sealed class SubmitReserveCommandValidator : AbstractValidator<SubmitReserveCommand>
{
    public SubmitReserveCommandValidator()
    {
        RuleFor(x => x.Amount)
            .Must((cmd, amount) => cmd.Component == ReserveComponentType.SubrogationRecoverable || amount > 0)
            .WithMessage("Reserve amount must be greater than zero."); // BR-R-01

        RuleFor(x => x.Amount).NotEqual(0m).WithMessage("Reserve amount must not be zero.");
        RuleFor(x => x.ChangeReason).NotEmpty().MaximumLength(500);
    }
}

public sealed class SubmitReserveCommandHandler(
    IClaimRepository claims,
    IBackgroundJobScheduler jobs,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork) : IRequestHandler<SubmitReserveCommand, SubmitReserveResult>
{
    public async Task<SubmitReserveResult> Handle(SubmitReserveCommand request, CancellationToken cancellationToken)
    {
        var claim = await claims.GetWithReservesAsync(request.ClaimId, cancellationToken)
            ?? throw new NotFoundException(nameof(Claim), request.ClaimId);

        if (claim.PolicyId is null)
            throw new BusinessRuleException("policy",
                "A policy must be linked before reserves can be set."); // BR-C-06

        var component = claim.ReserveComponents
                            .FirstOrDefault(c => c.Component == request.Component && c.Status == ReserveComponentStatus.Active)
                        ?? claim.AddReserveComponent(
                            ClaimReserveComponent.Open(claim.OrganisationId, claim.Id, request.Component));

        // BR-R-05: an auto-approved transaction that would push the claim total over the aggregate
        // limit needs a manager override (pending transactions are instead gated at approval time).
        if (ReserveAuthority.IsAutoApproved(request.Amount))
            ReserveOverride.Ensure(claim, request.Amount, currentUser, request.ApplyManagerOverride);

        var txn = component.SubmitTransaction(
            request.Amount, request.TransactionType, request.ChangeReason, currentUser.UserId);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (txn.ApprovalStatus == ReserveApprovalStatus.AutoApproved)
            await ReserveGlPosting.ScheduleAsync(jobs, unitOfWork, component, txn, cancellationToken); // FRS §6.5

        return new SubmitReserveResult
        {
            TransactionId = txn.Id,
            ReserveComponentId = component.Id,
            ApprovalStatus = txn.ApprovalStatus,
            AutoApproved = txn.ApprovalStatus == ReserveApprovalStatus.AutoApproved
        };
    }
}
