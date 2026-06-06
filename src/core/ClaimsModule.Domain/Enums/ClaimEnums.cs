namespace ClaimsModule.Domain.Enums;

/// <summary>Claim lifecycle states (FRS §4.1). Persisted as strings.</summary>
public enum ClaimStatus
{
    Draft,
    Open,
    UnderInvestigation,
    PendingPayment,
    Closed,
    Reopened,
    Withdrawn
}

/// <summary>Claim severity classification (FRS §9.1).</summary>
public enum ClaimSeverity
{
    Catastrophic,
    Critical,
    Standard,
    Minor
}

/// <summary>Severity of a validation issue (FRS §5.4). Critical blocks the Draft → Open transition.</summary>
public enum ValidationSeverity
{
    Critical,
    Warning
}
