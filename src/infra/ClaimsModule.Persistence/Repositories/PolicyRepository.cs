using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClaimsModule.Persistence.Repositories;

public sealed class PolicyRepository(ClaimsDbContext db) : IPolicyRepository
{
    private const int MaxResults = 50;

    public async Task<IReadOnlyList<Policy>> SearchAsync(string? query, CancellationToken cancellationToken = default)
    {
        var policies = db.Policies.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            policies = policies.Where(p => p.PolicyNumber.Contains(term) || p.ClientName.Contains(term));
        }

        return await policies.OrderBy(p => p.PolicyNumber).Take(MaxResults).ToListAsync(cancellationToken);
    }

    public Task<Policy?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Policies.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
}
