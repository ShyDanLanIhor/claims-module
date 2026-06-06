namespace ClaimsModule.Application.Common.Interfaces;

/// <summary>A file to be persisted to the configured storage provider.</summary>
public sealed record StorageSaveRequest(
    Guid OrganisationId,
    Guid ClaimId,
    string FileName,
    string ContentType,
    Stream Content);

/// <summary>The result of persisting a file: where it lives and how big it is.</summary>
public sealed record StoredFile(string BlobPath, long SizeBytes);

/// <summary>
/// Abstraction over document storage (FRS §13). Implemented by an Azure Blob provider and a local
/// filesystem fallback, selected via configuration so the API is testable and provider-agnostic.
/// </summary>
public interface IStorageService
{
    /// <summary>Stores the file under claim-documents/{org}/{claim}/{sanitised-name} and returns its path.</summary>
    Task<StoredFile> SaveAsync(StorageSaveRequest request, CancellationToken cancellationToken = default);

    /// <summary>Returns a short-lived (SAS) download URL; bytes are never proxied through the API.</summary>
    Task<Uri> GetDownloadUrlAsync(string blobPath, TimeSpan ttl, CancellationToken cancellationToken = default);

    /// <summary>Best-effort removal of a stored file; used to compensate an orphaned blob when the
    /// document metadata row fails to persist (the blob write and the DB write are not transactional).</summary>
    Task DeleteAsync(string blobPath, CancellationToken cancellationToken = default);
}
