using System.Security.Cryptography;
using System.Text;
using ClaimsModule.Domain.Common;

namespace ClaimsModule.Persistence.Seeding;

/// <summary>
/// Stable identifiers and helpers for EF Core data seeding (FRS §15.4: seed via HasData; a single
/// fixed OrganisationId per §15.1). Identity GUIDs are owned by <see cref="SeedIdentifiers"/> in the
/// Domain (a neutral home shared with the auth layer); re-exported here so HasData stays churn-free.
/// </summary>
public static class SeedConstants
{
    /// <summary>The single tenant the assessment data belongs to.</summary>
    public static readonly Guid OrganisationId = SeedIdentifiers.OrganisationId;

    /// <summary>Mock-auth identities (FRS §3); the Angular role switcher uses the same set.</summary>
    public static readonly Guid HandlerUserId = SeedIdentifiers.HandlerUserId;
    public static readonly Guid SupervisorUserId = SeedIdentifiers.SupervisorUserId;
    public static readonly Guid ManagerUserId = SeedIdentifiers.ManagerUserId;

    /// <summary>First and last calendar years for which claim-number sequences are pre-seeded.</summary>
    public const int FirstSequenceYear = 2024;
    public const int LastSequenceYear = 2035;

    /// <summary>Deterministic GUID derived from a stable string seed (so HasData keys never drift).</summary>
    public static Guid DeterministicGuid(string seed)
    {
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(seed));
        return new Guid(hash);
    }
}
