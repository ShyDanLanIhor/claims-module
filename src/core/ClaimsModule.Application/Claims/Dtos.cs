using ClaimsModule.Application.Common.Models;
using ClaimsModule.Application.Reserves;
using ClaimsModule.Domain.Enums;

namespace ClaimsModule.Application.Claims;

/// <summary>A row in the claims dashboard list (FRS §10.1 / §11.1).</summary>
public sealed record ClaimSummaryDto
{
    public Guid Id { get; init; }
    public string ClaimNumber { get; init; } = string.Empty;
    public string? PolicyNumber { get; init; }
    public string? ClientName { get; init; }
    public DateTimeOffset LossDate { get; init; }
    public string CauseOfLossCode { get; init; } = string.Empty;
    public ClaimStatus Status { get; init; }
    public decimal TotalReserves { get; init; }
    public Guid? AssignedHandlerId { get; init; }
}

public sealed record LossEventDto
{
    public DateTimeOffset LossDate { get; init; }
    public string LossDescription { get; init; } = string.Empty;
    public string? LossLocation { get; init; }
    public string CauseOfLossCode { get; init; } = string.Empty;
    public decimal? EstimatedLossAmount { get; init; }
    public DateTimeOffset ReportDate { get; init; }
    public string? PoliceReportNumber { get; init; }
}

public sealed record ClaimPartyDto
{
    public Guid Id { get; init; }
    public PartyRole PartyRole { get; init; }
    public PartyType PartyType { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? CompanyName { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Notes { get; init; }
    public bool IsActive { get; init; }
}

public sealed record ClaimRiskObjectDto
{
    public Guid Id { get; init; }
    public AssetType AssetType { get; init; }
    public string AssetDescription { get; init; } = string.Empty;
    public string? DamageDescription { get; init; }
    public bool IsPrimary { get; init; }
    public string? AssetReference { get; init; }
}

public sealed record AuditLogEntryDto
{
    public Guid Id { get; init; }
    public string EventType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? OldValue { get; init; }
    public string? NewValue { get; init; }
    public Guid? RelatedEntityId { get; init; }
    public string? RelatedEntityType { get; init; }
    public Guid? CorrelationId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public Guid? CreatedByUserId { get; init; }
}

/// <summary>Full claim record for the detail screen (FRS §10.1 GET /api/claims/{id}, §11.3).</summary>
public sealed record ClaimDetailDto
{
    public Guid Id { get; init; }
    public string ClaimNumber { get; init; } = string.Empty;
    public Guid? PolicyId { get; init; }
    public string? PolicyNumber { get; init; }
    public string? ClientName { get; init; }
    public ClaimStatus Status { get; init; }
    public ClaimSeverity? Severity { get; init; }
    public DateTimeOffset ReportedDate { get; init; }
    public Guid? AssignedHandlerId { get; init; }
    public DateTimeOffset? ClosedAt { get; init; }
    public string? ClosureReason { get; init; }
    public string? Notes { get; init; }
    public bool ManagerOverrideApplied { get; init; }

    public LossEventDto? LossEvent { get; init; }
    public IReadOnlyList<ClaimPartyDto> Parties { get; init; } = [];
    public IReadOnlyList<ClaimRiskObjectDto> RiskObjects { get; init; } = [];
    public IReadOnlyList<ReserveComponentSummaryDto> Reserves { get; init; } = [];
    public IReadOnlyList<ClaimDocumentMetadataDto> Documents { get; init; } = [];
    public IReadOnlyList<ClaimStatus> AllowedNextStatuses { get; init; } = [];
    public IReadOnlyList<AuditLogEntryDto> RecentAudit { get; init; } = [];
}

/// <summary>Result of FNOL creation: identity plus any non-blocking warnings (FRS §5.4).</summary>
public sealed record CreateClaimResult
{
    public Guid ClaimId { get; init; }
    public string ClaimNumber { get; init; } = string.Empty;
    public IReadOnlyList<ValidationIssue> Warnings { get; init; } = [];
}

/// <summary>Document metadata without a download URL (used inside claim detail).</summary>
public sealed record ClaimDocumentMetadataDto
{
    public Guid Id { get; init; }
    public string DocumentType { get; init; } = string.Empty;
    public string DocumentName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public long FileSizeBytes { get; init; }
    public DateTimeOffset UploadedAt { get; init; }
    public Guid? UploadedByUserId { get; init; }
    public string? Notes { get; init; }
}
