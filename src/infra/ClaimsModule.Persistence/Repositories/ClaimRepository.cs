using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClaimsModule.Persistence.Repositories;

/// <summary>Write-side claim repository — returns tracked aggregates with the graph each command needs.</summary>
public sealed class ClaimRepository(ClaimsDbContext db) : IClaimRepository
{
    public void Add(Claim claim) => db.Claims.Add(claim);

    public Task<Claim?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Claims.Include(c => c.LossEvent).FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<Claim?> GetWithPartiesAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Claims.Include(c => c.LossEvent).Include(c => c.Parties)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<Claim?> GetWithReservesAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Claims.Include(c => c.LossEvent)
            .Include(c => c.ReserveComponents).ThenInclude(rc => rc.History)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<Claim?> GetAggregateAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Claims.Include(c => c.LossEvent)
            .Include(c => c.Parties)
            .Include(c => c.RiskObjects)
            .Include(c => c.ReserveComponents).ThenInclude(rc => rc.History)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<bool> ClaimNumberExistsAsync(Guid organisationId, string claimNumber, CancellationToken cancellationToken = default) =>
        db.Claims.IgnoreQueryFilters() // soft-deleted claims still consume their number (BR-C-04)
            .AnyAsync(c => c.OrganisationId == organisationId && c.ClaimNumber == claimNumber, cancellationToken);
}
