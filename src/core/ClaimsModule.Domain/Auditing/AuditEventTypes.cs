namespace ClaimsModule.Domain.Auditing;

/// <summary>Canonical audit event-type constants (FRS §14.1). Avoids magic strings across the codebase.</summary>
public static class AuditEventTypes
{
    public const string ClaimCreated = "CLAIM_CREATED";
    public const string StatusChanged = "STATUS_CHANGED";
    public const string ClaimNotesUpdated = "CLAIM_NOTES_UPDATED";
    public const string PartyAdded = "PARTY_ADDED";
    public const string PartyRemoved = "PARTY_REMOVED";
    public const string ReserveCreated = "RESERVE_CREATED";
    public const string ReserveAutoApproved = "RESERVE_AUTO_APPROVED";
    public const string ReserveApproved = "RESERVE_APPROVED";
    public const string ReserveRejected = "RESERVE_REJECTED";
    public const string ReserveRetracted = "RESERVE_RETRACTED";
    public const string GlPostingSimulated = "GL_POSTING_SIMULATED";
    public const string GlPostingFailed = "GL_POSTING_FAILED";
    public const string GlPostingRetryRequested = "GL_POSTING_RETRY_REQUESTED";
    public const string DocumentUploaded = "DOCUMENT_UPLOADED";
    public const string ClaimClosed = "CLAIM_CLOSED";
    public const string ClaimReopened = "CLAIM_REOPENED";
    public const string SlaBreachDetected = "SLA_BREACH_DETECTED";
    public const string ValidationIssueAdded = "VALIDATION_ISSUE_ADDED";
}
