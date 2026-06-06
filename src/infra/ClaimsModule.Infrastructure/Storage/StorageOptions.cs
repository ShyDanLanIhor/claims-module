namespace ClaimsModule.Infrastructure.Storage;

/// <summary>Document storage configuration (FRS §13). Provider is selected via appsettings.json.</summary>
public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>"AzureBlob" or "LocalFileSystem".</summary>
    public string Provider { get; set; } = "LocalFileSystem";

    public string ContainerName { get; set; } = "claim-documents";

    /// <summary>Azure Storage connection string (required when Provider = AzureBlob).</summary>
    public string? ConnectionString { get; set; }

    /// <summary>Root directory for the local filesystem fallback.</summary>
    public string LocalRootPath { get; set; } = "uploads";

    /// <summary>Public base URL the API is reachable at, used to build local download links.</summary>
    public string? PublicBaseUrl { get; set; }
}
