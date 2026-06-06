using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Domain.Auditing;
using ClaimsModule.Domain.Enums;
using Hangfire.States;
using Hangfire.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ClaimsModule.Infrastructure.Jobs;

/// <summary>
/// Handles terminal failure of the GL posting job (FRS §12.1, §14.1). Hangfire applies
/// <see cref="FailedState"/> only once retries are exhausted (it reschedules instead while attempts
/// remain), so <c>OnStateApplied</c> with a Failed state is the genuine "all retries exhausted" hook.
/// At that point it sets PostingStatus = Failed and writes a GL_POSTING_FAILED audit entry — once.
/// </summary>
public sealed class GlPostingFailureStateFilter(IServiceScopeFactory scopeFactory) : IApplyStateFilter
{
    public void OnStateApplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
    {
        if (context.NewState is not FailedState failed)
            return;

        var job = context.BackgroundJob.Job;
        if (job is null
            || job.Method.Name != nameof(IGlPostingJob.PostAsync)
            || !typeof(IGlPostingJob).IsAssignableFrom(job.Type)
            || job.Args.Count == 0
            || job.Args[0] is not Guid reserveHistoryId)
            return;

        using var scope = scopeFactory.CreateScope();
        var sp = scope.ServiceProvider;
        var logger = sp.GetRequiredService<ILogger<GlPostingFailureStateFilter>>();

        try
        {
            var jobData = sp.GetRequiredService<IBackgroundJobData>();
            var txn = jobData.GetReserveTransactionAsync(reserveHistoryId).GetAwaiter().GetResult();
            if (txn is null || txn.PostingStatus is PostingStatus.Posted or PostingStatus.Failed)
                return;

            txn.ReserveComponent.MarkPostingFailed(txn);

            var reason = failed.Exception?.Message ?? "GL posting failed after all retries were exhausted.";
            sp.GetRequiredService<IAuditLogService>().WriteAsync(new AuditEntry(
                txn.ClaimId, AuditEventTypes.GlPostingFailed,
                $"GL posting failed after all retries: {reason}",
                NewValue: reason, RelatedEntityId: txn.Id, RelatedEntityType: "ReserveHistory"))
                .GetAwaiter().GetResult();

            sp.GetRequiredService<IUnitOfWork>().SaveChangesAsync().GetAwaiter().GetResult();
            logger.LogWarning("GL posting permanently failed for reserve transaction {Id}.", reserveHistoryId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to record GL posting failure for reserve transaction {Id}.", reserveHistoryId);
        }
    }

    public void OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction) { }
}
