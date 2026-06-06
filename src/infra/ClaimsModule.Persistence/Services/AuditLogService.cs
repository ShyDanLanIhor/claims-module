using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Domain.Entities;
using ClaimsModule.Persistence.Seeding;

namespace ClaimsModule.Persistence.Services;

/// <summary>
/// The single sanctioned writer for the append-only claim audit log (FRS §14.2). Stamps tenant,
/// actor, correlation id and timestamp; never updates or deletes existing entries.
/// </summary>
public sealed class AuditLogService(
    ClaimsDbContext db, ICurrentUserService currentUser, IDateTime clock) : IAuditLogService
{
    public Task WriteAsync(AuditEntry entry, CancellationToken cancellationToken = default)
    {
        db.ClaimAuditLogs.Add(Build(entry, ResolveActor()));
        return Task.CompletedTask;
    }

    public async Task WriteAndSaveAsync(AuditEntry entry, Guid? actorUserId, CancellationToken cancellationToken = default)
    {
        db.ClaimAuditLogs.Add(Build(entry, actorUserId));
        await db.SaveChangesAsync(cancellationToken);
    }

    private ClaimAuditLog Build(AuditEntry entry, Guid? actorUserId) => new()
    {
        OrganisationId = currentUser.OrganisationId == Guid.Empty ? SeedConstants.OrganisationId : currentUser.OrganisationId,
        ClaimId = entry.ClaimId,
        EventType = entry.EventType,
        Description = entry.Description,
        OldValue = entry.OldValue,
        NewValue = entry.NewValue,
        RelatedEntityId = entry.RelatedEntityId,
        RelatedEntityType = entry.RelatedEntityType,
        CorrelationId = currentUser.CorrelationId == Guid.Empty ? null : currentUser.CorrelationId,
        CreatedAt = clock.UtcNow,
        CreatedByUserId = actorUserId
    };

    private Guid? ResolveActor() => currentUser.UserId == Guid.Empty ? null : currentUser.UserId;
}
