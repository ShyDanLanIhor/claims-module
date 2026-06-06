using ClaimsModule.Domain.Common;

namespace ClaimsModule.Domain.Entities;

/// <summary>The loss occurrence linked to a claim (FRS §9.2). One loss event per claim.</summary>
public class LossEvent : AuditableEntity
{
    public Guid ClaimId { get; set; }

    public DateTimeOffset LossDate { get; set; }
    public string LossDescription { get; set; } = string.Empty;
    public string? LossLocation { get; set; }
    public string CauseOfLossCode { get; set; } = string.Empty;
    public decimal? EstimatedLossAmount { get; set; }
    public DateTimeOffset ReportDate { get; set; }
    public string? PoliceReportNumber { get; set; }

    public Claim Claim { get; set; } = null!;
}
