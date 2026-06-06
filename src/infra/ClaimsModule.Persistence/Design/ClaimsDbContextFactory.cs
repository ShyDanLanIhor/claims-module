using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ClaimsModule.Persistence.Design;

/// <summary>
/// Design-time factory used by the EF Core tools (dotnet ef migrations / database update). Migrations
/// are created against a placeholder connection string; the actual runtime connection comes from
/// configuration. A no-op publisher satisfies the DbContext constructor since no events are dispatched
/// during design-time operations.
/// </summary>
public sealed class ClaimsDbContextFactory : IDesignTimeDbContextFactory<ClaimsDbContext>
{
    public ClaimsDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("CLAIMSMODULE_DESIGN_CONNECTION")
            ?? "Server=(localdb)\\mssqllocaldb;Database=ClaimsModule;Trusted_Connection=True;MultipleActiveResultSets=true";

        var options = new DbContextOptionsBuilder<ClaimsDbContext>()
            .UseSqlServer(connectionString, sql => sql.MigrationsAssembly(typeof(ClaimsDbContext).Assembly.FullName))
            .Options;

        return new ClaimsDbContext(options, new NoOpPublisher());
    }

    private sealed class NoOpPublisher : IPublisher
    {
        public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification => Task.CompletedTask;
    }
}
