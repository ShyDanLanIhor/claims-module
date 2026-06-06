namespace ClaimsModule.Domain.Common;

/// <summary>
/// Fixed identity GUIDs for the single-tenant slice: the one seeded organisation and the three
/// mock-auth users (FRS §3, §15.1). These are an identity concern, so they live in the Domain — a
/// neutral home that both the auth layer (API) and EF data-seeding (Persistence) reference, rather
/// than auth reaching into a persistence-seeding helper for a request-time concept.
/// </summary>
public static class SeedIdentifiers
{
    public static readonly Guid OrganisationId = new("11111111-1111-1111-1111-111111111111");

    public static readonly Guid HandlerUserId = new("aaaaaaaa-0000-0000-0000-000000000001");
    public static readonly Guid SupervisorUserId = new("bbbbbbbb-0000-0000-0000-000000000002");
    public static readonly Guid ManagerUserId = new("cccccccc-0000-0000-0000-000000000003");
}
