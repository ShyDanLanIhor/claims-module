using ClaimsModule.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ClaimsModule.Api.IntegrationTests;

/// <summary>
/// Hosts the real API pipeline for tests, but swaps SQL Server for EF Core InMemory and disables the
/// Hangfire processing server and startup migrations, so HTTP-level tests run without infrastructure.
/// </summary>
public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    // A dedicated EF Core internal service provider for the InMemory provider. Without this, the app's
    // SqlServer provider services and the test InMemory services co-exist in one container and EF throws
    // "Only a single database provider can be registered" the first time a DbContext is resolved via DI.
    private static readonly IServiceProvider InMemoryEfServices =
        new ServiceCollection().AddEntityFrameworkInMemoryDatabase().BuildServiceProvider();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Hangfire:EnableServer", "false");
        builder.UseSetting("Database:ApplyMigrationsAtStartup", "false");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ClaimsDbContext>>();
            services.AddDbContext<ClaimsDbContext>(options => options
                .UseInMemoryDatabase("claims-integration-tests")
                .UseInternalServiceProvider(InMemoryEfServices));
        });
    }
}
