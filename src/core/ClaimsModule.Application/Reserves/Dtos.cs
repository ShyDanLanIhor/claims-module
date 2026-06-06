using ClaimsModule.Domain.Enums;

namespace ClaimsModule.Application.Reserves;

/// <summary>Current state of a reserve component (FRS §11.3 Reserves tab summary cards).</summary>
public sealed record ReserveComponentSummaryDto
{
    public Guid Id { get; init; }
    public ReserveComponentType Component { get; init; }
    public decimal CurrentAmount { get; init; }
    public decimal PendingAmount { get; init; }
    public ReserveComponentStatus Status { get; init; }
}

/// <summary>A single reserve transaction in the history table (FRS §9.6, §11.3).</summary>
public sealed record ReserveTransactionDto
{
    public Guid Id { get; init; }
    public Guid ReserveComponentId { get; init; }
    public ReserveComponentType Component { get; init; }
    public ReserveTransactionType TransactionType { get; init; }
    public decimal Amount { get; init; }
    public decimal PreviousBalance { get; init; }
    public decimal NewBalance { get; init; }
    public ReserveApprovalStatus ApprovalStatus { get; init; }
    public PostingStatus PostingStatus { get; init; }
    public string ChangeReason { get; init; } = string.Empty;
    public int ChangeSequence { get; init; }
    public Guid? SubmittedByUserId { get; init; }
    public Guid? ApprovedByUserId { get; init; }
    public Guid? RejectedByUserId { get; init; }
    public string? RejectionReason { get; init; }
    public string IdempotencyKey { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
}

/// <summary>Reserve summary + full history for a claim (FRS §10.2 GET /api/claims/{id}/reserves).</summary>
public sealed record ClaimReservesDto
{
    public IReadOnlyList<ReserveComponentSummaryDto> Components { get; init; } = [];
    public IReadOnlyList<ReserveTransactionDto> History { get; init; } = [];
    public decimal TotalApproved { get; init; }
    public decimal TotalPending { get; init; }
}

/// <summary>Result of submitting a reserve transaction (FRS §10.2 POST returns approval status).</summary>
public sealed record SubmitReserveResult
{
    public Guid TransactionId { get; init; }
    public Guid ReserveComponentId { get; init; }
    public ReserveApprovalStatus ApprovalStatus { get; init; }
    public bool AutoApproved { get; init; }
}
