using ClaimsModule.Domain.Entities;
using ClaimsModule.Persistence;
using ClaimsModule.Persistence.Seeding;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClaimsModule.Api.IntegrationTests;

/// <summary>
/// Locks in the multi-tenant global query filter (FRS §6.1 / §15.1): every read of an
/// <see cref="ClaimsModule.Domain.Common.ITenantScoped"/> entity is automatically scoped to the seeded
/// <see cref="SeedConstants.OrganisationId"/>, while global reference tables stay unfiltered. Runs the
/// real <see cref="ClaimsDbContext"/> model (so <c>OnModelCreating</c>'s filter is exercised) over the
/// EF Core InMemory provider, which honours global query filters.
/// </summary>
public sealed class TenantQueryFilterTests
{
    /// <summary>The DbContext drains and publishes domain events on save; tests don't need them.</summary>
    private sealed class NoOpPublisher : IPublisher
    {
        public Task Publish(object notification, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification => Task.CompletedTask;
    }

    private static ClaimsDbContext CreateContext(string databaseName) =>
        new(new DbContextOptionsBuilder<ClaimsDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options,
            new NoOpPublisher());

    [Fact]
    public async Task TenantScopedReads_Only_Return_Rows_Of_The_Seeded_Organisation()
    {
        var databaseName = $"{nameof(TenantQueryFilterTests)}-tenant-{Guid.NewGuid()}";
        var seededOrg = SeedConstants.OrganisationId;
        var otherOrg = Guid.NewGuid();

        // Arrange: two rows for the seeded tenant and one belonging to a different organisation.
        await using (var ctx = CreateContext(databaseName))
        {
            ctx.Policies.AddRange(
                new Policy { OrganisationId = seededOrg, PolicyNumber = "POL-1", ClientName = "Acme" },
                new Policy { OrganisationId = seededOrg, PolicyNumber = "POL-2", ClientName = "Globex" },
                new Policy { OrganisationId = otherOrg, PolicyNumber = "POL-X", ClientName = "Foreign Tenant" });
            await ctx.SaveChangesAsync();
        }

        // Act + Assert: a normal read sees only the seeded tenant's rows.
        await using (var ctx = CreateContext(databaseName))
        {
            var visible = await ctx.Policies.AsNoTracking().ToListAsync();

            Assert.Equal(2, visible.Count);
            Assert.All(visible, p => Assert.Equal(seededOrg, p.OrganisationId));
            Assert.DoesNotContain(visible, p => p.PolicyNumber == "POL-X");

            // The cross-tenant row physically exists — it is only hidden by the global query filter.
            var all = await ctx.Policies.IgnoreQueryFilters().AsNoTracking().ToListAsync();

            Assert.Equal(3, all.Count);
            Assert.Contains(all, p => p.PolicyNumber == "POL-X");
        }
    }

    [Fact]
    public async Task Reference_Lookup_Tables_Are_Not_Tenant_Filtered()
    {
        var databaseName = $"{nameof(TenantQueryFilterTests)}-reference-{Guid.NewGuid()}";

        // CauseOfLossCode is a global lookup (not ITenantScoped) — it carries no OrganisationId and must
        // remain visible regardless of tenant, otherwise FNOL cause selection would break across tenants.
        await using (var ctx = CreateContext(databaseName))
        {
            ctx.CauseOfLossCodes.AddRange(
                new CauseOfLossCode { Code = "FIRE", Name = "Fire" },
                new CauseOfLossCode { Code = "FLOOD", Name = "Flood" });
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = CreateContext(databaseName))
        {
            var codes = await ctx.CauseOfLossCodes.AsNoTracking().ToListAsync();

            Assert.Equal(2, codes.Count);
        }
    }
}
