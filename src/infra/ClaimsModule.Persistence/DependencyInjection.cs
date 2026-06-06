using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Persistence.Idempotency;
using ClaimsModule.Persistence.Interceptors;
using ClaimsModule.Persistence.Repositories;
using ClaimsModule.Persistence.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ClaimsModule.Persistence;

/// <summary>Registers the EF Core DbContext, the audit interceptor, repositories, the Unit of Work,
/// the claim-number generator and the audit-log service.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, string connectionString)
    {
        services.AddScoped<AuditableEntitySaveChangesInterceptor>();

        services.AddDbContext<ClaimsDbContext>((sp, options) =>
        {
            options.UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsAssembly(typeof(ClaimsDbContext).Assembly.FullName);
                // Detail/list reads eager-load several child collections; split queries avoid the
                // cartesian row explosion of a single multi-JOIN (PERSIST-03). SQL Server only.
                sql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            });
            options.AddInterceptors(sp.GetRequiredService<AuditableEntitySaveChangesInterceptor>());
        });

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IClaimRepository, ClaimRepository>();
        services.AddScoped<IClaimReadRepository, ClaimReadRepository>();
        services.AddScoped<IPolicyRepository, PolicyRepository>();
        services.AddScoped<IReferenceDataRepository, ReferenceDataRepository>();
        services.AddScoped<IBackgroundJobData, BackgroundJobDataRepository>();
        services.AddScoped<IClaimNumberGenerator, ClaimNumberGenerator>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IIdempotencyStore, IdempotencyStore>();

        return services;
    }
}
