using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using ClaimsModule.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace ClaimsModule.Infrastructure.Storage;

/// <summary>
/// Azure Blob Storage document provider (FRS §13). Uploads to the claim-documents container and
/// returns short-lived (1-hour) SAS URLs for retrieval — document bytes never flow through the API.
/// </summary>
public sealed class AzureBlobStorageService : IStorageService
{
    private readonly BlobContainerClient _container;

    public AzureBlobStorageService(IOptions<StorageOptions> options)
    {
        var opts = options.Value;
        var service = new BlobServiceClient(opts.ConnectionString);
        _container = service.GetBlobContainerClient(opts.ContainerName);
    }

    public async Task<StoredFile> SaveAsync(StorageSaveRequest request, CancellationToken cancellationToken = default)
    {
        await _container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var blobPath = StoragePath.Build(request.OrganisationId, request.ClaimId, request.FileName);
        var blob = _container.GetBlobClient(blobPath);

        await blob.UploadAsync(
            request.Content,
            new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = request.ContentType } },
            cancellationToken);

        var properties = await blob.GetPropertiesAsync(cancellationToken: cancellationToken);
        return new StoredFile(blobPath, properties.Value.ContentLength);
    }

    public Task<Uri> GetDownloadUrlAsync(string blobPath, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        var blob = _container.GetBlobClient(blobPath);

        if (!blob.CanGenerateSasUri)
            throw new InvalidOperationException(
                "A SAS URL cannot be generated. The blob client must be authenticated with a shared key.");

        var builder = new BlobSasBuilder(BlobSasPermissions.Read, DateTimeOffset.UtcNow.Add(ttl))
        {
            BlobContainerName = _container.Name,
            BlobName = blobPath,
            Resource = "b"
        };

        return Task.FromResult(blob.GenerateSasUri(builder));
    }

    public Task DeleteAsync(string blobPath, CancellationToken cancellationToken = default) =>
        _container.GetBlobClient(blobPath).DeleteIfExistsAsync(cancellationToken: cancellationToken);
}
