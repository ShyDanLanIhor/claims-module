using System.Linq.Expressions;

namespace ClaimsModule.Application.Common.Interfaces;

/// <summary>
/// Abstraction over the background job engine (Hangfire) so the Application layer can enqueue work
/// without depending on Hangfire directly.
/// </summary>
public interface IBackgroundJobScheduler
{
    /// <summary>Enqueues a fire-and-forget job and returns the engine's job id.</summary>
    string Enqueue<TJob>(Expression<Func<TJob, Task>> methodCall);

    /// <summary>Registers (or updates) a recurring job on the given CRON schedule.</summary>
    void AddOrUpdateRecurring<TJob>(string recurringJobId, Expression<Func<TJob, Task>> methodCall, string cronExpression);
}

/// <summary>
/// GL posting simulation job (FRS §6.5, §12.1). Idempotent and re-entrant safe via the
/// transaction's idempotency key.
/// </summary>
public interface IGlPostingJob
{
    Task PostAsync(Guid reserveHistoryId, CancellationToken cancellationToken = default);
}

/// <summary>SLA monitoring job (FRS §12.2). Recurring; flags stale Draft/Open claims in the audit log.</summary>
public interface ISlaMonitoringJob
{
    Task ScanAsync(CancellationToken cancellationToken = default);
}
