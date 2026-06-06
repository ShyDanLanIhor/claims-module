using System.Globalization;
using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Domain.Auditing;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace ClaimsModule.Infrastructure.Jobs;

/// <summary>
/// GL posting simulation — the FRS "PostGLReserveChangeJob" (§6.5, §12.1). Idempotent and re-entrant
/// safe: if the transaction is already Posted it no-ops; otherwise it writes a single
/// GL_POSTING_SIMULATED audit entry, marks the transaction Posted, and commits both atomically.
/// Transient failures are retried by Hangfire; terminal failure (retries exhausted) is handled by
/// <see cref="GlPostingFailureStateFilter"/>, which writes PostingStatus = Failed + GL_POSTING_FAILED.
/// </summary>
[AutomaticRetry(Attempts = 3)]
public sealed class PostGlReserveChangeJob(
    IBackgroundJobData jobData,
    IAuditLogService auditLog,
    IUnitOfWork unitOfWork,
    ILogger<PostGlReserveChangeJob> logger) : IGlPostingJob
{
    public async Task PostAsync(Guid reserveHistoryId, CancellationToken cancellationToken = default)
    {
        var txn = await jobData.GetReserveTransactionAsync(reserveHistoryId, cancellationToken);
        if (txn is null)
        {
            logger.LogWarning("GL posting skipped: reserve transaction {Id} not found.", reserveHistoryId);
            return;
        }

        // Idempotency guard (FRS §12.1): a transaction already posted produces no duplicate entry.
        if (txn.PostingStatus == Domain.Enums.PostingStatus.Posted)
        {
            logger.LogInformation("GL posting skipped: {Key} already posted.", txn.IdempotencyKey);
            return;
        }

        if (!txn.IsEffective)
        {
            logger.LogWarning("GL posting skipped: {Key} is not approved.", txn.IdempotencyKey);
            return;
        }

        var amount = txn.Amount.ToString("C", CultureInfo.GetCultureInfo("en-US"));
        var journal =
            $"DR Change in Outstanding Reserves / CR Outstanding Loss Reserves, Amount = {amount} " +
            $"[{txn.IdempotencyKey}]";

        await auditLog.WriteAsync(new AuditEntry(
            txn.ClaimId, AuditEventTypes.GlPostingSimulated, journal,
            NewValue: journal, RelatedEntityId: txn.Id, RelatedEntityType: "ReserveHistory"), cancellationToken);

        txn.ReserveComponent.MarkPosted(txn);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger.LogInformation("GL posting simulated for {Key}.", txn.IdempotencyKey);
    }
}
