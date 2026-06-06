using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Application.Common.Models;
using ClaimsModule.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClaimsModule.Persistence.Repositories;

/// <summary>Read-side claim repository (AsNoTracking projections for queries — CQRS read path).</summary>
public sealed class ClaimReadRepository(ClaimsDbContext db) : IClaimReadRepository
{
    public async Task<(IReadOnlyList<Claim> Items, int Total)> ListAsync(ClaimListFilter filter, CancellationToken cancellationToken = default)
    {
        var query = db.Claims.AsNoTracking()
            .Include(c => c.LossEvent)
            .Include(c => c.ReserveComponents)
            .AsQueryable();

        if (filter.Statuses is { Count: > 0 } statuses)
            query = query.Where(c => statuses.Contains(c.Status));

        if (filter.LossDateFrom is { } from)
            query = query.Where(c => c.LossEvent.LossDate >= from);

        if (filter.LossDateTo is { } to)
            query = query.Where(c => c.LossEvent.LossDate <= to);

        if (filter.AssignedHandlerId is { } handler)
            query = query.Where(c => c.AssignedHandlerId == handler);

        if (!string.IsNullOrWhiteSpace(filter.CauseOfLossCode))
            query = query.Where(c => c.LossEvent.CauseOfLossCode == filter.CauseOfLossCode);

        if (filter.PolicyId is { } policyId)
            query = query.Where(c => c.PolicyId == policyId);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(c => c.ClaimNumber.Contains(term) || (c.ClientName != null && c.ClientName.Contains(term)));
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(c => c.ReportedDate)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public Task<Claim?> GetDetailAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Claims.AsNoTracking()
            .Include(c => c.LossEvent)
            .Include(c => c.Parties)
            .Include(c => c.RiskObjects)
            .Include(c => c.ReserveComponents).ThenInclude(rc => rc.History)
            .Include(c => c.Documents)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ClaimReserveComponent>> GetReserveComponentsAsync(Guid claimId, CancellationToken cancellationToken = default) =>
        await db.ClaimReserveComponents.AsNoTracking()
            .Include(c => c.History)
            .Where(c => c.ClaimId == claimId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ClaimDocument>> GetDocumentsAsync(Guid claimId, CancellationToken cancellationToken = default) =>
        await db.ClaimDocuments.AsNoTracking()
            .Where(d => d.ClaimId == claimId)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync(cancellationToken);

    public async Task<(IReadOnlyList<ClaimAuditLog> Items, int Total)> GetAuditLogAsync(Guid claimId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = db.ClaimAuditLogs.AsNoTracking().Where(a => a.ClaimId == claimId);
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, total);
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Claims.AsNoTracking().AnyAsync(c => c.Id == id, cancellationToken);
}
