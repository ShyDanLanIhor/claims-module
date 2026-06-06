using ClaimsModule.Persistence;
using ClaimsModule.Persistence.Idempotency;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClaimsModule.Api.IntegrationTests;

/// <summary>
/// Verifies the idempotency store (FRS §10) round-trips a recorded response and is tenant-scoped, using
/// the real <see cref="ClaimsDbContext"/> model over EF Core InMemory.
/// </summary>
public sealed class IdempotencyStoreTests
{
    private sealed class NoOpPublisher : IPublisher
    {
        public Task Publish(object notification, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification => Task.CompletedTask;
    }

    private static ClaimsDbContext CreateContext(string databaseName) =>
        new(new DbContextOptionsBuilder<ClaimsDbContext>().UseInMemoryDatabase(databaseName).Options, new NoOpPublisher());

    [Fact]
    public async Task Save_Then_TryGet_Replays_The_Response_Scoped_To_The_Tenant()
    {
        var databaseName = $"{nameof(IdempotencyStoreTests)}-{Guid.NewGuid()}";
        var org = Guid.NewGuid();
        const string key = "idem-key-123";

        await using (var ctx = CreateContext(databaseName))
        {
            var store = new IdempotencyStore(ctx);
            await store.SaveAsync(org, key, "POST", "/api/claims", 201, "{\"id\":\"x\"}", "application/json");
        }

        await using (var ctx = CreateContext(databaseName))
        {
            var store = new IdempotencyStore(ctx);

            var hit = await store.TryGetAsync(org, key);
            Assert.NotNull(hit);
            Assert.Equal(201, hit!.StatusCode);
            Assert.Equal("{\"id\":\"x\"}", hit.Body);
            Assert.Equal("application/json", hit.ContentType);

            // Per-tenant scoping: the same key in another organisation is a miss.
            Assert.Null(await store.TryGetAsync(Guid.NewGuid(), key));
            // An unseen key is a miss.
            Assert.Null(await store.TryGetAsync(org, "different-key"));
        }
    }
}
