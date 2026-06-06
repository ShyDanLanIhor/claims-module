using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Domain.Auditing;
using Microsoft.Extensions.Logging;

namespace ClaimsModule.Infrastructure.Jobs;

/// <summary>
/// Recurring SLA monitor (FRS §12.2). Flags Draft/Open claims not updated in 48 hours by writing an
/// SLA_BREACH_DETECTED audit entry. De-duplicates so a claim is not re-flagged within 24 hours. Does
/// not change claim status.
/// </summary>
public sealed class SlaMonitoringJob(
    IBackgroundJobData jobData,
    IAuditLogService auditLog,
    IDateTime clock,
    ILogger<SlaMonitoringJob> logger) : ISlaMonitoringJob
{
    private static readonly TimeSpan StaleThreshold = TimeSpan.FromHours(48);
    private static readonly TimeSpan ReflagInterval = TimeSpan.FromHours(24);

    public async Task ScanAsync(CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;
        var staleClaims = await jobData.GetStaleClaimsAsync(now - StaleThreshold, cancellationToken);

        var flagged = 0;
        foreach (var claim in staleClaims)
        {
            var lastBreach = await jobData.GetLastSlaBreachAtAsync(claim.Id, cancellationToken);
            if (lastBreach is { } last && now - last < ReflagInterval)
                continue;

            await auditLog.WriteAndSaveAsync(new AuditEntry(
                claim.Id, AuditEventTypes.SlaBreachDetected,
                "Claim has not been updated in 48 hours."), actorUserId: null, cancellationToken);
            flagged++;
        }

        logger.LogInformation("SLA scan complete: {Flagged} of {Total} stale claim(s) flagged.",
            flagged, staleClaims.Count);
    }
}
