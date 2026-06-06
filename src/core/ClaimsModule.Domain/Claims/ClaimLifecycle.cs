using ClaimsModule.Domain.Enums;

namespace ClaimsModule.Domain.Claims;

/// <summary>
/// The claim status state machine (FRS §4.2). This is the single source of truth for which
/// transitions are permitted; the <c>ClaimStatusTransitions</c> reference table is seeded from
/// the same definition so the API can expose valid next-statuses to the frontend.
/// </summary>
public static class ClaimLifecycle
{
    /// <summary>Allowed (From → To) transitions and the minimum role they require, if any.</summary>
    public static readonly IReadOnlyList<ClaimTransition> Transitions =
    [
        new(ClaimStatus.Draft, ClaimStatus.Open),
        new(ClaimStatus.Open, ClaimStatus.UnderInvestigation),
        new(ClaimStatus.Open, ClaimStatus.PendingPayment),
        new(ClaimStatus.Open, ClaimStatus.Closed),
        new(ClaimStatus.Open, ClaimStatus.Withdrawn),
        new(ClaimStatus.UnderInvestigation, ClaimStatus.Open),
        new(ClaimStatus.UnderInvestigation, ClaimStatus.PendingPayment),
        new(ClaimStatus.UnderInvestigation, ClaimStatus.Closed),
        new(ClaimStatus.UnderInvestigation, ClaimStatus.Withdrawn),
        new(ClaimStatus.PendingPayment, ClaimStatus.Closed),
        new(ClaimStatus.Closed, ClaimStatus.Reopened, UserRole.Supervisor),
        new(ClaimStatus.Reopened, ClaimStatus.Open)
    ];

    public static bool IsAllowed(ClaimStatus from, ClaimStatus to) =>
        Transitions.Any(t => t.From == from && t.To == to);

    public static IReadOnlyList<ClaimStatus> AllowedNext(ClaimStatus from) =>
        Transitions.Where(t => t.From == from).Select(t => t.To).ToList();

    public static UserRole? RequiredRole(ClaimStatus from, ClaimStatus to) =>
        Transitions.FirstOrDefault(t => t.From == from && t.To == to)?.RequiredRole;
}

/// <summary>A single permitted status transition, with an optional minimum role.</summary>
public sealed record ClaimTransition(ClaimStatus From, ClaimStatus To, UserRole? RequiredRole = null);
