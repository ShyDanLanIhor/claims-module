using ClaimsModule.Application.Common.Exceptions;
using ClaimsModule.Application.Documents;
using ClaimsModule.Domain.Documents;
using Microsoft.AspNetCore.Mvc;

namespace ClaimsModule.API.Controllers;

[Route("api/claims/{claimId:guid}/documents")]
public sealed class DocumentsController : ApiControllerBase
{
    /// <summary>Upload a document (multipart). Stored in blob storage; metadata persisted (FRS §10.1, §13).</summary>
    [HttpPost]
    [RequestSizeLimit(60_000_000)]
    public async Task<ActionResult<Guid>> Upload(
        Guid claimId,
        IFormFile file,
        [FromForm] string documentType = DocumentTypes.Default,
        [FromForm] string? notes = null,
        CancellationToken ct = default)
    {
        if (file is null || file.Length == 0)
            throw new BusinessRuleException("file", "A non-empty file is required."); // §10.4 envelope (422)

        await using var stream = file.OpenReadStream();
        var documentId = await Mediator.Send(new UploadClaimDocumentCommand
        {
            ClaimId = claimId,
            FileName = file.FileName,
            ContentType = file.ContentType,
            Length = file.Length,
            Content = stream,
            DocumentType = documentType,
            Notes = notes
        }, ct);

        return CreatedAtAction(nameof(List), new { claimId }, documentId);
    }

    /// <summary>List documents, each with a short-lived download URL.</summary>
    [HttpGet]
    public Task<IReadOnlyList<ClaimDocumentDto>> List(Guid claimId, CancellationToken ct) =>
        Mediator.Send(new ListClaimDocumentsQuery(claimId), ct);
}
