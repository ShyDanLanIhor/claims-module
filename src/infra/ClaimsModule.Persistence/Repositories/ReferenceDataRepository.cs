using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Domain.Entities;
using ClaimsModule.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ClaimsModule.Persistence.Repositories;

public sealed class ReferenceDataRepository(ClaimsDbContext db) : IReferenceDataRepository
{
    public async Task<IReadOnlyList<CauseOfLossCode>> GetCauseOfLossCodesAsync(
        PerilCategory? perilCategory, bool activeOnly, CancellationToken cancellationToken = default)
    {
        var query = db.CauseOfLossCodes.AsNoTracking().AsQueryable();

        if (activeOnly)
            query = query.Where(c => c.IsActive);

        if (perilCategory is { } category)
            query = query.Where(c => c.PerilCategory == category);

        return await query.OrderBy(c => c.SortOrder).ToListAsync(cancellationToken);
    }

    public Task<bool> CauseOfLossCodeIsActiveAsync(string code, CancellationToken cancellationToken = default) =>
        db.CauseOfLossCodes.AsNoTracking().AnyAsync(c => c.Code == code && c.IsActive, cancellationToken);

    public async Task<IReadOnlyList<ClaimStatusTransition>> GetStatusTransitionsAsync(CancellationToken cancellationToken = default) =>
        await db.ClaimStatusTransitions.AsNoTracking().ToListAsync(cancellationToken);
}
