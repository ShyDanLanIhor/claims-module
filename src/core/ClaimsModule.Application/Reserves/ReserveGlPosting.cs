using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Domain.Entities;

namespace ClaimsModule.Application.Reserves;

/// <summary>
/// The single home for the ordering-sensitive GL-posting schedule used by every reserve path: enqueue
/// the Hangfire job, then persist the returned job id (FRS §6.5/§12.1) with a second save. Must be
/// called AFTER the reserve transaction has committed.
/// </summary>
internal static class ReserveGlPosting
{
    public static async Task ScheduleAsync(
        IBackgroundJobScheduler jobs,
        IUnitOfWork unitOfWork,
        ClaimReserveComponent component,
        ReserveHistory txn,
        CancellationToken cancellationToken)
    {
        var jobId = jobs.Enqueue<IGlPostingJob>(j => j.PostAsync(txn.Id, default));
        component.RecordPostingJob(txn, jobId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
