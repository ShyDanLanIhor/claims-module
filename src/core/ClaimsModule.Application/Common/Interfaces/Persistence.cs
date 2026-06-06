using ClaimsModule.Application.Common.Models;
using ClaimsModule.Domain.Entities;
using ClaimsModule.Domain.Enums;

namespace ClaimsModule.Application.Common.Interfaces;

/// <summary>
/// Coordinates persistence for a request. SaveChanges drains domain events and writes audit
/// entries within the same transaction (FRS: Unit of Work coordinates persistence).
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>Runs <paramref name="action"/> inside a database transaction (used by FNOL create).</summary>
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default);
}

/// <summary>Write-side access to the claim aggregate (tracked, with the graph each command needs).</summary>
public interface IClaimRepository
{
    void Add(Claim claim);

    Task<Claim?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Claim?> GetWithPartiesAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Claim?> GetWithReservesAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Loads the full aggregate (loss event, parties, reserve components + history) for
    /// cross-entity operations such as status transitions and closure-condition checks.</summary>
    Task<Claim?> GetAggregateAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ClaimNumberExistsAsync(Guid organisationId, string claimNumber, CancellationToken cancellationToken = default);
}

/// <summary>Read-side (AsNoTracking) access for queries — kept separate to signal CQRS intent.</summary>
public interface IClaimReadRepository
{
    Task<(IReadOnlyList<Claim> Items, int Total)> ListAsync(ClaimListFilter filter, CancellationToken cancellationToken = default);
    Task<Claim?> GetDetailAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClaimReserveComponent>> GetReserveComponentsAsync(Guid claimId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClaimDocument>> GetDocumentsAsync(Guid claimId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<ClaimAuditLog> Items, int Total)> GetAuditLogAsync(Guid claimId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>
/// Data access for the Hangfire background jobs (FRS §12), exposed through the Application boundary
/// so the job definitions in Infrastructure never depend on EF Core directly.
/// </summary>
public interface IBackgroundJobData
{
    /// <summary>Loads a reserve transaction (tracked, with its component) for GL posting.</summary>
    Task<ReserveHistory?> GetReserveTransactionAsync(Guid reserveHistoryId, CancellationToken cancellationToken = default);

    /// <summary>Claims in Draft/Open whose last update is older than the threshold (SLA scan).</summary>
    Task<IReadOnlyList<Claim>> GetStaleClaimsAsync(DateTimeOffset updatedBefore, CancellationToken cancellationToken = default);

    /// <summary>Timestamp of the most recent SLA breach entry for a claim (de-dupes breach events).</summary>
    Task<DateTimeOffset?> GetLastSlaBreachAtAsync(Guid claimId, CancellationToken cancellationToken = default);
}

/// <summary>Access to the simulated policy dataset (FRS §5.5, §9.10).</summary>
public interface IPolicyRepository
{
    Task<IReadOnlyList<Policy>> SearchAsync(string? query, CancellationToken cancellationToken = default);
    Task<Policy?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>Access to seeded reference data (FRS §9.9, §3.2 ClaimStatusTransitions).</summary>
public interface IReferenceDataRepository
{
    Task<IReadOnlyList<CauseOfLossCode>> GetCauseOfLossCodesAsync(PerilCategory? perilCategory, bool activeOnly, CancellationToken cancellationToken = default);
    Task<bool> CauseOfLossCodeIsActiveAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClaimStatusTransition>> GetStatusTransitionsAsync(CancellationToken cancellationToken = default);
}
