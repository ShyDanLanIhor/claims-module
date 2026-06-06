using ClaimsModule.Domain.Common;
using ClaimsModule.Domain.Enums;

namespace ClaimsModule.Domain.Entities;

/// <summary>An asset affected by the loss (FRS §9.4). Simplified — type + description, no deep hierarchy.</summary>
public class ClaimRiskObject : AuditableEntity
{
    public Guid ClaimId { get; set; }

    public AssetType AssetType { get; set; }
    public string AssetDescription { get; set; } = string.Empty;
    public string? DamageDescription { get; set; }
    public bool IsPrimary { get; set; }
    public string? AssetReference { get; set; }

    public Claim Claim { get; set; } = null!;
}
