using ClaimsModule.Domain.Enums;

namespace ClaimsModule.Domain.Reserves;

/// <summary>
/// Encapsulates the three-tier reserve authority rules (FRS §6.3, BR-R-02) and the aggregate
/// override limit (BR-R-05). The tier is decided by the magnitude of the individual transaction,
/// not the running total.
/// </summary>
public static class ReserveAuthority
{
    /// <summary>Transactions at or below this are auto-approved for any role.</summary>
    public const decimal AutoApprovalLimit = 10_000m;

    /// <summary>Above auto-approval and up to this limit requires a Supervisor (or Manager).</summary>
    public const decimal SupervisorLimit = 100_000m;

    /// <summary>Total approved reserves per claim above this require a Manager override flag.</summary>
    public const decimal AggregateOverrideLimit = 10_000_000m;

    /// <summary>
    /// Magnitude used for tiering. SubrogationRecoverable transactions may be negative
    /// (FRS §6.2); authority is judged on absolute value.
    /// </summary>
    private static decimal Magnitude(decimal amount) => Math.Abs(amount);

    public static bool IsAutoApproved(decimal amount) => Magnitude(amount) <= AutoApprovalLimit;

    /// <summary>The minimum role that may authorise a transaction of this amount.</summary>
    public static UserRole RequiredRole(decimal amount)
    {
        var m = Magnitude(amount);
        if (m <= AutoApprovalLimit) return UserRole.Handler;
        if (m <= SupervisorLimit) return UserRole.Supervisor;
        return UserRole.Manager;
    }

    /// <summary>Approval status a brand-new transaction of this amount should be created with.</summary>
    public static ReserveApprovalStatus InitialApprovalStatus(decimal amount) =>
        IsAutoApproved(amount) ? ReserveApprovalStatus.AutoApproved : ReserveApprovalStatus.PendingApproval;

    /// <summary>Whether the given role has the authority to approve a transaction of this amount.</summary>
    public static bool CanApprove(UserRole role, decimal amount) => role switch
    {
        UserRole.Manager => true,
        UserRole.Supervisor => Magnitude(amount) <= SupervisorLimit,
        _ => false
    };
}
