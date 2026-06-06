using ClaimsModule.Domain.Common;
using ClaimsModule.Domain.Enums;

namespace ClaimsModule.Domain.Entities;

/// <summary>
/// Simulated policy (FRS §9.10, §5.5). Stands in for a real Policy module; seeded with a
/// minimal dataset so FNOL can perform policy lookup and effective-period validation.
/// </summary>
public class Policy : BaseEntity, ITenantScoped
{
    public Guid OrganisationId { get; set; }
    public string PolicyNumber { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public DateOnly EffectiveDate { get; set; }
    public DateOnly ExpirationDate { get; set; }
    public PolicyStatus Status { get; set; }

    /// <summary>Comma-separated coverage type names (e.g. "Vehicle,Cargo"). Simplified per scope.</summary>
    public string CoverageTypes { get; set; } = string.Empty;

    /// <summary>True if <paramref name="lossDate"/> falls within the policy effective period (BR-C-02).</summary>
    public bool CoversDate(DateOnly lossDate) =>
        lossDate >= EffectiveDate && lossDate <= ExpirationDate;
}
