using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Domain.Auditing;
using ClaimsModule.Domain.Entities;
using ClaimsModule.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ClaimsModule.Persistence.Repositories;

public sealed class BackgroundJobDataRepository(ClaimsDbContext db) : IBackgroundJobData
{
    public Task<ReserveHistory?> GetReserveTransactionAsync(Guid reserveHistoryId, CancellationToken cancellationToken = default) =>
        db.ReserveHistory
            .Include(h => h.ReserveComponent) // tracked: the job marks the transaction posted
            .FirstOrDefaultAsync(h => h.Id == reserveHistoryId, cancellationToken);

    public async Task<IReadOnlyList<Claim>> GetStaleClaimsAsync(DateTimeOffset updatedBefore, CancellationToken cancellationToken = default) =>
        await db.Claims.AsNoTracking()
            .Where(c => (c.Status == ClaimStatus.Draft || c.Status == ClaimStatus.Open)
                        && (c.UpdatedAt ?? c.CreatedAt) < updatedBefore)
            .ToListAsync(cancellationToken);

    public async Task<DateTimeOffset?> GetLastSlaBreachAtAsync(Guid claimId, CancellationToken cancellationToken = default)
    {
        var last = await db.ClaimAuditLogs.AsNoTracking()
            .Where(a => a.ClaimId == claimId && a.EventType == AuditEventTypes.SlaBreachDetected)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => (DateTimeOffset?)a.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return last;
    }
}
