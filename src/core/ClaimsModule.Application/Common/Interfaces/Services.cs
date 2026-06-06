using ClaimsModule.Domain.Enums;

namespace ClaimsModule.Application.Common.Interfaces;

/// <summary>Abstraction over the system clock for testability. All times are UTC.</summary>
public interface IDateTime
{
    DateTimeOffset UtcNow { get; }
}

/// <summary>
/// The authenticated caller for the current request. Backed by the JWT/mock auth context;
/// supplies tenant, identity, role and correlation id for audit propagation (FRS §14.2).
/// </summary>
public interface ICurrentUserService
{
    Guid UserId { get; }
    string? UserName { get; }
    UserRole Role { get; }
    Guid OrganisationId { get; }
    Guid CorrelationId { get; }
    bool IsInRole(UserRole role);
}

/// <summary>
/// Generates atomic, gap-free claim numbers per organisation/year in the format
/// CLM-{YYYY}-{7-digit} (FRS §5.3, BR-C-04).
/// </summary>
public interface IClaimNumberGenerator
{
    Task<string> NextAsync(Guid organisationId, int year, CancellationToken cancellationToken = default);
}

/// <summary>A single entry destined for the append-only claim audit log (FRS §14).</summary>
public sealed record AuditEntry(
    Guid ClaimId,
    string EventType,
    string Description,
    string? OldValue = null,
    string? NewValue = null,
    Guid? RelatedEntityId = null,
    string? RelatedEntityType = null);

/// <summary>
/// The only sanctioned path for writing to the claim audit log (FRS §14.2). Implementations stamp
/// tenant, actor, correlation id and timestamp and append (never update/delete) the entry.
/// </summary>
public interface IAuditLogService
{
    /// <summary>Enlists an audit entry into the current unit of work (saved with the request's transaction).</summary>
    Task WriteAsync(AuditEntry entry, CancellationToken cancellationToken = default);

    /// <summary>Writes and persists an audit entry immediately — used by background jobs (FRS §12).</summary>
    Task WriteAndSaveAsync(AuditEntry entry, Guid? actorUserId, CancellationToken cancellationToken = default);
}
