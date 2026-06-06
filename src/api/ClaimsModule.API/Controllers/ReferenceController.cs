using ClaimsModule.Application.Reference;
using ClaimsModule.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace ClaimsModule.API.Controllers;

[Route("api/reference")]
public sealed class ReferenceController : ApiControllerBase
{
    /// <summary>Active cause-of-loss codes, optionally filtered by peril category (FRS §10.3).</summary>
    [HttpGet("cause-of-loss-codes")]
    public Task<IReadOnlyList<CauseOfLossCodeDto>> GetCauseOfLossCodes(
        [FromQuery] PerilCategory? perilCategory, CancellationToken ct) =>
        Mediator.Send(new ListCauseOfLossCodesQuery(perilCategory), ct);

    /// <summary>Claim statuses with their valid next-status transitions.</summary>
    [HttpGet("claim-statuses")]
    public Task<IReadOnlyList<ClaimStatusInfoDto>> GetClaimStatuses(CancellationToken ct) =>
        Mediator.Send(new ListClaimStatusesQuery(), ct);

    /// <summary>Accepted document categories for the document-upload picker.</summary>
    [HttpGet("document-types")]
    public Task<IReadOnlyList<string>> GetDocumentTypes(CancellationToken ct) =>
        Mediator.Send(new ListDocumentTypesQuery(), ct);
}
