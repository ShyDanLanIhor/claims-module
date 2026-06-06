using ClaimsModule.Domain.Common;
using ClaimsModule.Domain.Enums;

namespace ClaimsModule.Domain.Entities;

/// <summary>
/// Reference table of allowed status transitions (FRS §3.2, §4.2). Seeded from
/// <see cref="Claims.ClaimLifecycle"/> so the /api/reference/claim-statuses endpoint can expose
/// valid next-statuses to the frontend.
/// </summary>
public class ClaimStatusTransition : BaseEntity
{
    public ClaimStatus FromStatus { get; set; }
    public ClaimStatus ToStatus { get; set; }

    /// <summary>Minimum role required for the transition, if any (e.g. Reopen requires Supervisor).</summary>
    public UserRole? RequiredRole { get; set; }
}
