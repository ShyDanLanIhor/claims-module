using ClaimsModule.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClaimsModule.Persistence.Idempotency;

/// <summary>EF Core-backed <see cref="IIdempotencyStore"/> (FRS §10).</summary>
public sealed class IdempotencyStore(ClaimsDbContext db) : IIdempotencyStore
{
    public async Task<IdempotentResponse?> TryGetAsync(Guid organisationId, string key, CancellationToken cancellationToken = default)
    {
        var record = await db.IdempotencyRecords.AsNoTracking()
            .FirstOrDefaultAsync(r => r.OrganisationId == organisationId && r.Key == key, cancellationToken);

        return record is null ? null : new IdempotentResponse(record.StatusCode, record.ResponseBody, record.ContentType);
    }

    public async Task SaveAsync(Guid organisationId, string key, string method, string path,
        int statusCode, string? body, string? contentType, CancellationToken cancellationToken = default)
    {
        db.IdempotencyRecords.Add(new IdempotencyRecord
        {
            OrganisationId = organisationId,
            Key = key,
            Method = method,
            Path = path,
            StatusCode = statusCode,
            ResponseBody = body,
            ContentType = contentType,
            CreatedAt = DateTimeOffset.UtcNow
        });

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // A concurrent request with the same key already recorded a response — safe to ignore.
        }
    }
}
