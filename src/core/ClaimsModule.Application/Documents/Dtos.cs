namespace ClaimsModule.Application.Documents;

/// <summary>Document metadata plus a short-lived download URL (FRS §10.1 GET .../documents, §13).</summary>
public sealed record ClaimDocumentDto
{
    public Guid Id { get; init; }
    public string DocumentType { get; init; } = string.Empty;
    public string DocumentName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public long FileSizeBytes { get; init; }
    public DateTimeOffset UploadedAt { get; init; }
    public Guid? UploadedByUserId { get; init; }
    public string? Notes { get; init; }

    /// <summary>SAS (or local) URL with a 1-hour TTL. Bytes are never proxied through the API.</summary>
    public string DownloadUrl { get; init; } = string.Empty;
}
