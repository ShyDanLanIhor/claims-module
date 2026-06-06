using ClaimsModule.Domain.Common;
using ClaimsModule.Domain.Enums;

namespace ClaimsModule.Domain.Entities;

/// <summary>A person or organisation involved in a claim (FRS §9.3).</summary>
public class ClaimParty : AuditableEntity
{
    public Guid ClaimId { get; set; }

    public PartyRole PartyRole { get; set; }
    public PartyType PartyType { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? CompanyName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Notes { get; set; }

    /// <summary>Soft-removal from the claim (FRS §10.1, DELETE party). Distinct from row-level soft delete.
    /// Flipped only by <see cref="Claim.RemoveParty"/>; internal setter keeps that the single path.</summary>
    public bool IsActive { get; internal set; } = true;

    public Claim Claim { get; set; } = null!;

    /// <summary>Human-readable name for audit descriptions and UI.</summary>
    public string DisplayName => PartyType == PartyType.Company
        ? CompanyName ?? "(unnamed company)"
        : string.Join(' ', new[] { FirstName, LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));
}
