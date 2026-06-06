using ClaimsModule.Application.Common.Exceptions;
using ClaimsModule.Application.Common.Interfaces;
using MediatR;

namespace ClaimsModule.Application.Documents;

/// <summary>Lists a claim's documents, each with a short-lived download URL
/// (FRS §10.1 GET /api/claims/{id}/documents).</summary>
public sealed record ListClaimDocumentsQuery(Guid ClaimId) : IRequest<IReadOnlyList<ClaimDocumentDto>>;

public sealed class ListClaimDocumentsQueryHandler(
    IClaimReadRepository claims, IStorageService storage)
    : IRequestHandler<ListClaimDocumentsQuery, IReadOnlyList<ClaimDocumentDto>>
{
    private static readonly TimeSpan UrlTtl = TimeSpan.FromHours(1); // FRS §13: 1-hour SAS TTL

    public async Task<IReadOnlyList<ClaimDocumentDto>> Handle(ListClaimDocumentsQuery request, CancellationToken cancellationToken)
    {
        if (!await claims.ExistsAsync(request.ClaimId, cancellationToken))
            throw new NotFoundException(nameof(Domain.Entities.Claim), request.ClaimId);

        var documents = await claims.GetDocumentsAsync(request.ClaimId, cancellationToken);

        var result = new List<ClaimDocumentDto>(documents.Count);
        foreach (var d in documents)
        {
            var url = await storage.GetDownloadUrlAsync(d.BlobPath, UrlTtl, cancellationToken);
            result.Add(new ClaimDocumentDto
            {
                Id = d.Id,
                DocumentType = d.DocumentType,
                DocumentName = d.DocumentName,
                ContentType = d.ContentType,
                FileSizeBytes = d.FileSizeBytes,
                UploadedAt = d.UploadedAt,
                UploadedByUserId = d.UploadedByUserId,
                Notes = d.Notes,
                DownloadUrl = url.ToString()
            });
        }

        return result;
    }
}
