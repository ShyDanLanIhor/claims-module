using ClaimsModule.Application.Common.Exceptions;
using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Domain.Documents;
using ClaimsModule.Domain.Entities;
using FluentValidation;
using MediatR;

namespace ClaimsModule.Application.Documents;

/// <summary>Uploads a document to a claim (FRS §10.1 POST /api/claims/{id}/documents, §13).</summary>
public sealed record UploadClaimDocumentCommand : IRequest<Guid>
{
    public Guid ClaimId { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public long Length { get; init; }
    public required Stream Content { get; init; }
    public string DocumentType { get; init; } = DocumentTypes.Default;
    public string? Notes { get; init; }
}

/// <summary>Stateless input validation (FRS §13) runs in the MediatR pipeline like every other command:
/// content-type allowlist, document-type acceptance, and the 50 MB size limit.</summary>
public sealed class UploadClaimDocumentCommandValidator : AbstractValidator<UploadClaimDocumentCommand>
{
    private const long MaxFileSizeBytes = 50L * 1024 * 1024; // FRS §13: reasonable 50 MB limit

    // FRS §13: allowlist of acceptable content types.
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "image/jpeg",
        "image/png",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document", // .docx
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",       // .xlsx
        "text/plain",
        "text/csv"
    };

    public UploadClaimDocumentCommandValidator()
    {
        RuleFor(x => x.ContentType).Must(AllowedContentTypes.Contains)
            .WithMessage(x => $"Content type '{x.ContentType}' is not permitted.");

        // DocumentTypes.All is the single source of truth (also served by GET /api/reference/document-types).
        RuleFor(x => x.DocumentType).Must(DocumentTypes.IsAccepted)
            .WithMessage(x => $"Document type '{x.DocumentType}' is not recognised.");

        RuleFor(x => x.Length).LessThanOrEqualTo(MaxFileSizeBytes)
            .WithMessage("File exceeds the maximum allowed size of 50 MB.");
    }
}

public sealed class UploadClaimDocumentCommandHandler(
    IClaimRepository claims,
    IStorageService storage,
    ICurrentUserService currentUser,
    IDateTime clock,
    IUnitOfWork unitOfWork) : IRequestHandler<UploadClaimDocumentCommand, Guid>
{
    public async Task<Guid> Handle(UploadClaimDocumentCommand request, CancellationToken cancellationToken)
    {
        var claim = await claims.GetAsync(request.ClaimId, cancellationToken)
            ?? throw new NotFoundException(nameof(Claim), request.ClaimId);

        var stored = await storage.SaveAsync(new StorageSaveRequest(
            claim.OrganisationId, claim.Id, request.FileName, request.ContentType, request.Content), cancellationToken);

        try
        {
            var document = claim.AddDocument(new ClaimDocument
            {
                DocumentType = request.DocumentType,
                DocumentName = request.FileName,
                BlobPath = stored.BlobPath,
                ContentType = request.ContentType,
                FileSizeBytes = stored.SizeBytes,
                UploadedAt = clock.UtcNow,
                UploadedByUserId = currentUser.UserId,
                Notes = request.Notes
            });

            await unitOfWork.SaveChangesAsync(cancellationToken);
            return document.Id;
        }
        catch
        {
            // The blob is already written but the metadata row failed — compensate to avoid an orphan.
            await storage.DeleteAsync(stored.BlobPath, cancellationToken);
            throw;
        }
    }
}
