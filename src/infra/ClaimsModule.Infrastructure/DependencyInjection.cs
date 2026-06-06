using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Infrastructure.Jobs;
using ClaimsModule.Infrastructure.Services;
using ClaimsModule.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ClaimsModule.Infrastructure;

/// <summary>Registers infrastructure services: the clock, the storage provider (Azure Blob or local
/// fallback, chosen from configuration), the Hangfire job scheduler and the job definitions.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IDateTime, SystemDateTime>();

        var storageSection = configuration.GetSection(StorageOptions.SectionName);
        services.Configure<StorageOptions>(storageSection);
        var storageOptions = storageSection.Get<StorageOptions>() ?? new StorageOptions();

        var useAzure = string.Equals(storageOptions.Provider, "AzureBlob", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(storageOptions.ConnectionString);

        if (useAzure)
            services.AddScoped<IStorageService, AzureBlobStorageService>();
        else
            services.AddScoped<IStorageService, LocalFileSystemStorageService>();

        services.AddSingleton<IBackgroundJobScheduler, HangfireBackgroundJobScheduler>();
        services.AddScoped<IGlPostingJob, PostGlReserveChangeJob>();
        services.AddScoped<ISlaMonitoringJob, SlaMonitoringJob>();
        services.AddSingleton<GlPostingFailureStateFilter>(); // terminal GL-failure handler (Hangfire filter)

        return services;
    }
}
