using ClaimsModule.Domain.Common;
using ClaimsModule.Domain.Enums;

namespace ClaimsModule.Domain.Entities;

/// <summary>
/// Append-only reserve transaction (FRS §6.6, §9.6). A reserve is never updated in place — each
/// change is a new row, and a component's current balance is the sum of its posted/approved
/// transactions. Approval and GL-posting state live on the transaction itself.
/// </summary>
public class ReserveHistory : BaseEntity, ITenantScoped
{
    // State below is owned by ClaimReserveComponent (same assembly) and written only through its
    // methods — setters are internal so no outer layer can mutate a transaction directly (DDD §9.6).
    public Guid OrganisationId { get; set; } // ITenantScoped (public contract)
    public Guid ReserveComponentId { get; internal set; }

    /// <summary>Denormalised from the component for query convenience (FRS §9.6).</summary>
    public Guid ClaimId { get; internal set; }

    public ReserveTransactionType TransactionType { get; internal set; }
    public decimal Amount { get; internal set; }
    public decimal PreviousBalance { get; internal set; }
    public decimal NewBalance { get; internal set; }

    public ReserveApprovalStatus ApprovalStatus { get; internal set; }
    public Guid? ApprovedByUserId { get; internal set; }
    public DateTimeOffset? ApprovedAt { get; internal set; }
    public Guid? RejectedByUserId { get; internal set; }
    public DateTimeOffset? RejectedAt { get; internal set; }
    public string? RejectionReason { get; internal set; }
    public string ChangeReason { get; internal set; } = string.Empty;

    public PostingStatus PostingStatus { get; internal set; }
    public string? PostingJobId { get; internal set; }
    public string IdempotencyKey { get; internal set; } = string.Empty;

    /// <summary>Monotonically increasing per component; part of the GL idempotency key.</summary>
    public int ChangeSequence { get; internal set; }

    public Guid? SubmittedByUserId { get; internal set; }
    public DateTimeOffset CreatedAt { get; internal set; }

    public ClaimReserveComponent ReserveComponent { get; set; } = null!;

    /// <summary>True once approval has been granted (auto or manual) — i.e. it counts toward balance.</summary>
    public bool IsEffective =>
        ApprovalStatus is ReserveApprovalStatus.AutoApproved or ReserveApprovalStatus.Approved;
}
