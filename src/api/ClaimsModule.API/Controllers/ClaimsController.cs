using ClaimsModule.Application.Claims;
using ClaimsModule.Application.Common.Models;
using ClaimsModule.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace ClaimsModule.API.Controllers;

[Route("api/claims")]
public sealed class ClaimsController : ApiControllerBase
{
    /// <summary>FNOL intake — create a new claim (FRS §10.1).</summary>
    [HttpPost]
    public async Task<ActionResult<CreateClaimResult>> Create(CreateClaimCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.ClaimId }, result);
    }

    /// <summary>Paginated, filterable list of claims.</summary>
    [HttpGet]
    public Task<PagedResult<ClaimSummaryDto>> List(
        [FromQuery(Name = "status")] ClaimStatus[]? statuses,
        [FromQuery] DateTimeOffset? dateFrom,
        [FromQuery] DateTimeOffset? dateTo,
        [FromQuery] Guid? assignedHandlerId,
        [FromQuery] string? causeOfLossCode,
        [FromQuery] Guid? policyId,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default) =>
        Mediator.Send(new ListClaimsQuery
        {
            Statuses = statuses,
            LossDateFrom = dateFrom,
            LossDateTo = dateTo,
            AssignedHandlerId = assignedHandlerId,
            CauseOfLossCode = causeOfLossCode,
            PolicyId = policyId,
            Search = search,
            Page = page,
            PageSize = pageSize
        }, ct);

    /// <summary>Full claim detail.</summary>
    [HttpGet("{id:guid}")]
    public Task<ClaimDetailDto> GetById(Guid id, CancellationToken ct) =>
        Mediator.Send(new GetClaimDetailQuery(id), ct);

    /// <summary>Transition the claim's status (FRS §4.2).</summary>
    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> ChangeStatus(Guid id, ChangeStatusRequest request, CancellationToken ct)
    {
        await Mediator.Send(new ChangeClaimStatusCommand
        {
            ClaimId = id,
            TargetStatus = request.TargetStatus,
            Reason = request.Reason,
            AcknowledgeWarnings = request.AcknowledgeWarnings
        }, ct);
        return NoContent();
    }

    /// <summary>Update the claim's free-text notes (FRS §11.3 editable notes field).</summary>
    [HttpPut("{id:guid}/notes")]
    public async Task<IActionResult> UpdateNotes(Guid id, UpdateNotesRequest request, CancellationToken ct)
    {
        await Mediator.Send(new UpdateClaimNotesCommand { ClaimId = id, Notes = request.Notes }, ct);
        return NoContent();
    }

    /// <summary>Paginated, reverse-chronological audit log.</summary>
    [HttpGet("{id:guid}/audit")]
    public Task<PagedResult<AuditLogEntryDto>> GetAudit(
        Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default) =>
        Mediator.Send(new GetClaimAuditLogQuery(id, page, pageSize), ct);

    /// <summary>Add a party to the claim.</summary>
    [HttpPost("{id:guid}/parties")]
    public async Task<ActionResult<Guid>> AddParty(Guid id, AddPartyRequest request, CancellationToken ct)
    {
        var partyId = await Mediator.Send(new AddClaimPartyCommand
        {
            ClaimId = id,
            PartyRole = request.PartyRole,
            PartyType = request.PartyType,
            FirstName = request.FirstName,
            LastName = request.LastName,
            CompanyName = request.CompanyName,
            Email = request.Email,
            Phone = request.Phone,
            Notes = request.Notes
        }, ct);
        return CreatedAtAction(nameof(GetById), new { id }, partyId);
    }

    /// <summary>Soft-remove a party (returns 422 when removing the last Claimant).</summary>
    [HttpDelete("{id:guid}/parties/{partyId:guid}")]
    public async Task<IActionResult> RemoveParty(Guid id, Guid partyId, CancellationToken ct)
    {
        await Mediator.Send(new RemoveClaimPartyCommand(id, partyId), ct);
        return NoContent();
    }
}

public sealed record ChangeStatusRequest(ClaimStatus TargetStatus, string? Reason, bool AcknowledgeWarnings = false);

public sealed record UpdateNotesRequest(string? Notes);

public sealed record AddPartyRequest(
    PartyRole PartyRole,
    PartyType PartyType,
    string? FirstName,
    string? LastName,
    string? CompanyName,
    string? Email,
    string? Phone,
    string? Notes);
