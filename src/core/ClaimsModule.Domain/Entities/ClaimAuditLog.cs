using ClaimsModule.Domain.Common;

namespace ClaimsModule.Domain.Entities;

/// <summary>
/// Immutable, append-only event log entry for a claim (FRS §9.8, §14). Writes go exclusively
/// through IAuditLogService; no UPDATE or DELETE is ever performed on this table.
/// </summary>
public class ClaimAuditLog : BaseEntity, ITenantScoped
{
    public Guid OrganisationId { get; set; }
    public Guid ClaimId { get; set; }

    public string EventType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public Guid? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; }
    public Guid? CorrelationId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
}
