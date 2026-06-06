using ClaimsModule.Domain.Common;

namespace ClaimsModule.Domain.Entities;

/// <summary>
/// Per-organisation, per-year atomic counter backing claim-number generation (FRS §5.3, BR-C-04).
/// The current value is incremented under an atomic UPDATE so numbers are unique and gap-free even
/// under concurrent FNOL submissions; soft-deleted claims still consume their sequence number.
/// </summary>
public class ClaimNumberSequence : BaseEntity, ITenantScoped
{
    public Guid OrganisationId { get; set; }

    /// <summary>Calendar year the sequence belongs to (claim numbers reset per year).</summary>
    public int Year { get; set; }

    /// <summary>Last allocated value; the next claim takes <c>CurrentValue + 1</c>.</summary>
    public long CurrentValue { get; set; }
}
