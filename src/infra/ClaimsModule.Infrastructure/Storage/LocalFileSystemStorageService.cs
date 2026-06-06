using ClaimsModule.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace ClaimsModule.Infrastructure.Storage;

/// <summary>
/// Local filesystem fallback for document storage (FRS §13). Files are written under
/// {LocalRootPath}/{org}/{claim}/{file} and served by the API's static-file middleware at /uploads.
/// </summary>
public sealed class LocalFileSystemStorageService(IOptions<StorageOptions> options) : IStorageService
{
    private readonly StorageOptions _options = options.Value;

    public async Task<StoredFile> SaveAsync(StorageSaveRequest request, CancellationToken cancellationToken = default)
    {
        var blobPath = StoragePath.Build(request.OrganisationId, request.ClaimId, request.FileName);
        var fullPath = Path.Combine(_options.LocalRootPath, blobPath.Replace('/', Path.DirectorySeparatorChar));

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using (var file = File.Create(fullPath))
        {
            await request.Content.CopyToAsync(file, cancellationToken);
        }

        return new StoredFile(blobPath, new FileInfo(fullPath).Length);
    }

    public Task<Uri> GetDownloadUrlAsync(string blobPath, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        // No SAS for local storage; return a URL the static-file middleware serves.
        if (!string.IsNullOrWhiteSpace(_options.PublicBaseUrl))
            return Task.FromResult(new Uri(new Uri(_options.PublicBaseUrl), $"uploads/{blobPath}"));

        var fullPath = Path.GetFullPath(Path.Combine(_options.LocalRootPath, blobPath.Replace('/', Path.DirectorySeparatorChar)));
        return Task.FromResult(new Uri(fullPath));
    }

    public Task DeleteAsync(string blobPath, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_options.LocalRootPath, blobPath.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(fullPath))
            File.Delete(fullPath);
        return Task.CompletedTask;
    }
}
