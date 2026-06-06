using ClaimsModule.Domain.Common;
using ClaimsModule.Domain.Enums;

namespace ClaimsModule.Domain.Entities;

/// <summary>Reference/lookup table for cause-of-loss codes (FRS §9.9). Seeded at migration time.</summary>
public class CauseOfLossCode : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public PerilCategory PerilCategory { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}
