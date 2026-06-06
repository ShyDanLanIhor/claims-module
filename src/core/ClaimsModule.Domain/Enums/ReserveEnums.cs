namespace ClaimsModule.Domain.Enums;

/// <summary>Reserve component classification (FRS §6.2). Only SubrogationRecoverable may be negative.</summary>
public enum ReserveComponentType
{
    Indemnity,
    Expense,
    ALAE,
    SubrogationRecoverable
}

/// <summary>Lifecycle of a reserve component (FRS §9.5).</summary>
public enum ReserveComponentStatus
{
    Active,
    Closed
}

/// <summary>Approval state of an individual reserve transaction (FRS §9.6).</summary>
public enum ReserveApprovalStatus
{
    AutoApproved,
    PendingApproval,
    Approved,
    Rejected,
    Cancelled
}

/// <summary>Nature of a reserve transaction in the append-only history (FRS §9.6).</summary>
public enum ReserveTransactionType
{
    Add,
    Adjust,
    Reverse
}

/// <summary>GL posting state of a reserve transaction (FRS §6.5, §12.1).</summary>
public enum PostingStatus
{
    Pending,
    Posted,
    Failed,
    Cancelled
}
