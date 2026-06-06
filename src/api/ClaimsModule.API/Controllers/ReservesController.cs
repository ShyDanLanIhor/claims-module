using ClaimsModule.Application.Reserves;
using Microsoft.AspNetCore.Mvc;

namespace ClaimsModule.API.Controllers;

[Route("api/claims/{claimId:guid}/reserves")]
public sealed class ReservesController : ApiControllerBase
{
    /// <summary>Open or adjust a reserve component (FRS §10.2). Returns 201 with the created transaction.</summary>
    [HttpPost]
    public async Task<ActionResult<SubmitReserveResult>> Submit(Guid claimId, SubmitReserveRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new SubmitReserveCommand
        {
            ClaimId = claimId,
            Component = request.Component,
            Amount = request.Amount,
            ChangeReason = request.ChangeReason,
            TransactionType = request.TransactionType,
            ApplyManagerOverride = request.ApplyManagerOverride
        }, ct);
        return CreatedAtAction(nameof(Get), new { claimId }, result);
    }

    /// <summary>Reserve summary and full transaction history for the claim.</summary>
    [HttpGet]
    public Task<ClaimReservesDto> Get(Guid claimId, CancellationToken ct) =>
        Mediator.Send(new GetClaimReservesQuery(claimId), ct);

    /// <summary>Approve a pending reserve (Supervisor/Manager; no self-approval).</summary>
    [HttpPost("{transactionId:guid}/approve")]
    public async Task<IActionResult> Approve(Guid claimId, Guid transactionId, ApproveReserveRequest? request, CancellationToken ct)
    {
        await Mediator.Send(new ApproveReserveCommand
        {
            ClaimId = claimId,
            TransactionId = transactionId,
            ApplyManagerOverride = request?.ApplyManagerOverride ?? false
        }, ct);
        return NoContent();
    }

    /// <summary>Reject a pending reserve (Supervisor/Manager).</summary>
    [HttpPost("{transactionId:guid}/reject")]
    public async Task<IActionResult> Reject(Guid claimId, Guid transactionId, RejectReserveRequest request, CancellationToken ct)
    {
        await Mediator.Send(new RejectReserveCommand
        {
            ClaimId = claimId,
            TransactionId = transactionId,
            RejectionReason = request.RejectionReason
        }, ct);
        return NoContent();
    }

    /// <summary>Submitter retracts their own pending reserve.</summary>
    [HttpPost("{transactionId:guid}/retract")]
    public async Task<IActionResult> Retract(Guid claimId, Guid transactionId, CancellationToken ct)
    {
        await Mediator.Send(new RetractReserveCommand(claimId, transactionId), ct);
        return NoContent();
    }

    /// <summary>Retry GL posting for a reserve whose posting previously failed (Supervisor/Manager).</summary>
    [HttpPost("{transactionId:guid}/retry-gl-posting")]
    public async Task<IActionResult> RetryGlPosting(Guid claimId, Guid transactionId, CancellationToken ct)
    {
        await Mediator.Send(new RetryGlPostingCommand(claimId, transactionId), ct);
        return NoContent();
    }
}

public sealed record SubmitReserveRequest(
    Domain.Enums.ReserveComponentType Component,
    decimal Amount,
    string ChangeReason,
    Domain.Enums.ReserveTransactionType TransactionType = Domain.Enums.ReserveTransactionType.Add,
    bool ApplyManagerOverride = false);

public sealed record ApproveReserveRequest(bool ApplyManagerOverride = false);

public sealed record RejectReserveRequest(string RejectionReason);
