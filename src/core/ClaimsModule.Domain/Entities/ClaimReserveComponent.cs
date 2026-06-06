using ClaimsModule.Domain.Common;
using ClaimsModule.Domain.Enums;
using ClaimsModule.Domain.Events;
using ClaimsModule.Domain.Reserves;

namespace ClaimsModule.Domain.Entities;

/// <summary>
/// A reserve component on a claim (FRS §9.5) and an aggregate root for the reserve ledger. The
/// current value is the sum of effective (auto-approved/approved) transactions in
/// <see cref="History"/>; the component is never mutated except through the methods below, which
/// append a transaction and keep <see cref="CurrentAmount"/> consistent.
/// </summary>
public class ClaimReserveComponent : AuditableEntity, IAggregateRoot
{
    private readonly List<ReserveHistory> _history = [];

    public Guid ClaimId { get; set; }
    public ReserveComponentType Component { get; set; }
    public decimal CurrentAmount { get; private set; }
    public ReserveComponentStatus Status { get; private set; } = ReserveComponentStatus.Active;
    public string? Notes { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public Claim Claim { get; set; } = null!;
    public IReadOnlyCollection<ReserveHistory> History => _history.AsReadOnly();

    private ClaimReserveComponent() { }

    /// <summary>Opens a new, empty (zero-balance) reserve component.</summary>
    public static ClaimReserveComponent Open(Guid organisationId, Guid claimId, ReserveComponentType component) => new()
    {
        OrganisationId = organisationId,
        ClaimId = claimId,
        Component = component,
        Status = ReserveComponentStatus.Active,
        CurrentAmount = 0m
    };

    /// <summary>Sum of transactions still awaiting approval (UI shows this as a pending overlay).</summary>
    public decimal PendingAmount =>
        _history.Where(h => h.ApprovalStatus == ReserveApprovalStatus.PendingApproval).Sum(h => h.Amount);

    /// <summary>
    /// Appends a transaction (FRS §6.3–§6.4). Amounts at or below the auto-approval limit are
    /// effective immediately; larger amounts are created PendingApproval and do not move the balance
    /// until approved. Returns the created transaction so the handler can enqueue GL posting.
    /// </summary>
    public ReserveHistory SubmitTransaction(
        decimal amount,
        ReserveTransactionType transactionType,
        string changeReason,
        Guid submittedByUserId)
    {
        var sequence = (_history.Count == 0 ? 0 : _history.Max(h => h.ChangeSequence)) + 1;
        var approvalStatus = ReserveAuthority.InitialApprovalStatus(amount);
        var isEffective = approvalStatus == ReserveApprovalStatus.AutoApproved;
        var previousBalance = CurrentAmount;
        var newBalance = isEffective ? previousBalance + amount : previousBalance;

        var txn = new ReserveHistory
        {
            OrganisationId = OrganisationId,
            ReserveComponentId = Id,
            ClaimId = ClaimId,
            TransactionType = transactionType,
            Amount = amount,
            PreviousBalance = previousBalance,
            NewBalance = newBalance,
            ApprovalStatus = approvalStatus,
            ChangeReason = changeReason,
            ChangeSequence = sequence,
            IdempotencyKey = $"Reserve:{Id}:Change:{sequence}",
            PostingStatus = PostingStatus.Pending,
            SubmittedByUserId = submittedByUserId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        if (isEffective)
        {
            txn.ApprovedByUserId = submittedByUserId;
            txn.ApprovedAt = txn.CreatedAt;
            CurrentAmount = newBalance;
        }

        _history.Add(txn);
        RaiseDomainEvent(new ReserveSubmittedDomainEvent(
            ClaimId, Id, txn.Id, Component, amount, approvalStatus, isEffective));
        return txn;
    }

    /// <summary>Approves a pending transaction (FRS §6.4). Balance moves at approval time.</summary>
    public void Approve(ReserveHistory txn, Guid approverUserId)
    {
        EnsureBelongs(txn);
        if (txn.ApprovalStatus != ReserveApprovalStatus.PendingApproval)
            throw new DomainException("Only a pending reserve transaction can be approved.");

        txn.ApprovalStatus = ReserveApprovalStatus.Approved;
        txn.ApprovedByUserId = approverUserId;
        txn.ApprovedAt = DateTimeOffset.UtcNow;
        txn.PreviousBalance = CurrentAmount;
        txn.NewBalance = CurrentAmount + txn.Amount;
        CurrentAmount = txn.NewBalance;

        RaiseDomainEvent(new ReserveApprovedDomainEvent(ClaimId, txn.Id, txn.Amount, approverUserId));
    }

    /// <summary>Rejects a pending transaction (FRS §6.4). The record is retained in history.</summary>
    public void Reject(ReserveHistory txn, Guid approverUserId, string rejectionReason)
    {
        EnsureBelongs(txn);
        if (txn.ApprovalStatus != ReserveApprovalStatus.PendingApproval)
            throw new DomainException("Only a pending reserve transaction can be rejected.");

        txn.ApprovalStatus = ReserveApprovalStatus.Rejected;
        txn.RejectedByUserId = approverUserId;
        txn.RejectedAt = DateTimeOffset.UtcNow;
        txn.RejectionReason = rejectionReason;
        txn.PostingStatus = PostingStatus.Cancelled;

        RaiseDomainEvent(new ReserveRejectedDomainEvent(ClaimId, txn.Id, approverUserId, rejectionReason));
    }

    /// <summary>Submitter retracts their own pending transaction before approval (FRS §6.4 rule).</summary>
    public void Retract(ReserveHistory txn, Guid userId)
    {
        EnsureBelongs(txn);
        if (txn.ApprovalStatus != ReserveApprovalStatus.PendingApproval)
            throw new DomainException("Only a pending reserve transaction can be retracted.");

        txn.ApprovalStatus = ReserveApprovalStatus.Cancelled;
        txn.PostingStatus = PostingStatus.Cancelled;

        RaiseDomainEvent(new ReserveRetractedDomainEvent(ClaimId, txn.Id, userId));
    }

    /// <summary>Marks a transaction as posted to the GL (called by the Hangfire posting job).</summary>
    public void MarkPosted(ReserveHistory txn)
    {
        EnsureBelongs(txn);
        txn.PostingStatus = PostingStatus.Posted;
    }

    /// <summary>Records the Hangfire job id that owns this transaction's GL posting (FRS §12.1),
    /// captured from the scheduler at enqueue time.</summary>
    public void RecordPostingJob(ReserveHistory txn, string jobId)
    {
        EnsureBelongs(txn);
        txn.PostingJobId = jobId;
    }

    /// <summary>Marks GL posting as failed after Hangfire retries are exhausted (FRS §12.1).</summary>
    public void MarkPostingFailed(ReserveHistory txn)
    {
        EnsureBelongs(txn);
        txn.PostingStatus = PostingStatus.Failed;
    }

    /// <summary>
    /// Re-queues a previously failed GL posting for another attempt (FRS §11.3 / §12.1). Only a
    /// transaction whose posting Failed can be retried; it returns to Pending so the idempotent
    /// posting job can run again. The balance is unaffected (the transaction is already effective).
    /// </summary>
    public void RetryGlPosting(ReserveHistory txn, Guid requestedByUserId)
    {
        EnsureBelongs(txn);
        if (txn.PostingStatus != PostingStatus.Failed)
            throw new DomainException("Only a failed GL posting can be retried.");

        txn.PostingStatus = PostingStatus.Pending;
        RaiseDomainEvent(new ReserveGlRetryRequestedDomainEvent(ClaimId, txn.Id, requestedByUserId));
    }

    private void EnsureBelongs(ReserveHistory txn)
    {
        if (txn.ReserveComponentId != Id)
            throw new DomainException("Reserve transaction does not belong to this component.");
    }
}
