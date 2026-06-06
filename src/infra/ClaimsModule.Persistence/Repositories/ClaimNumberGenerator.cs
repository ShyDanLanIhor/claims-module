using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace ClaimsModule.Persistence.Repositories;

/// <summary>
/// Generates claim numbers atomically (FRS §5.3, BR-C-04). An <c>UPDATE … OUTPUT</c> increments the
/// per-org/year counter and returns the new value in one statement; running inside the FNOL
/// transaction, the row lock serialises concurrent submissions so numbers are unique and gap-free.
/// </summary>
public sealed class ClaimNumberGenerator(ClaimsDbContext db) : IClaimNumberGenerator
{
    public async Task<string> NextAsync(Guid organisationId, int year, CancellationToken cancellationToken = default)
    {
        const string updateSql =
            "UPDATE ClaimNumberSequences SET CurrentValue = CurrentValue + 1 " +
            "OUTPUT INSERTED.CurrentValue AS Value " +
            "WHERE OrganisationId = {0} AND Year = {1}";

        var updated = await db.Database
            .SqlQueryRaw<long>(updateSql, organisationId, year)
            .ToListAsync(cancellationToken);

        long next;
        if (updated.Count > 0)
        {
            next = updated[0];
        }
        else
        {
            // Fallback for a year outside the pre-seeded range (unique index guards against races).
            // Ids are client-assigned COMB GUIDs (ValueGeneratedNever), so the Id must be supplied here.
            await db.Database.ExecuteSqlRawAsync(
                "INSERT INTO ClaimNumberSequences (Id, OrganisationId, Year, CurrentValue) VALUES ({0}, {1}, {2}, 1)",
                [SequentialGuid.Create(), organisationId, year], cancellationToken);
            next = 1;
        }

        return $"CLM-{year}-{next:D7}";
    }
}
