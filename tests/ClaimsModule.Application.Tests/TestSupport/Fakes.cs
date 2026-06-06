using System.Linq.Expressions;
using ClaimsModule.Application.Common.Exceptions;
using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Domain.Entities;
using ClaimsModule.Domain.Enums;

namespace ClaimsModule.Application.Tests.TestSupport;

// Hand-rolled test doubles (the project deliberately uses no mocking library — see CreateClaimCommandValidatorTests).

internal sealed class FakeCurrentUser : ICurrentUserService
{
    public Guid UserId { get; set; } = Guid.NewGuid();
    public string? UserName { get; set; } = "test.user";
    public UserRole Role { get; set; } = UserRole.Handler;
    public Guid OrganisationId { get; set; } = TestData.Org;
    public Guid CorrelationId { get; } = Guid.NewGuid();
    public bool IsInRole(UserRole role) => (int)Role >= (int)role;
}

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCount++;
        return Task.FromResult(1);
    }

    public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default)
        => action(cancellationToken);
}

internal sealed class FakeClaimRepository(Claim? claim = null) : IClaimRepository
{
    public Claim? Claim { get; set; } = claim;
    public Claim? Added { get; private set; }

    public void Add(Claim claim) => Added = claim;
    public Task<Claim?> GetAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(Claim);
    public Task<Claim?> GetWithPartiesAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(Claim);
    public Task<Claim?> GetWithReservesAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(Claim);
    public Task<Claim?> GetAggregateAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(Claim);
    public Task<bool> ClaimNumberExistsAsync(Guid organisationId, string claimNumber, CancellationToken cancellationToken = default) => Task.FromResult(false);
}

internal sealed class FakePolicyRepository(Policy? policy = null) : IPolicyRepository
{
    public Policy? Policy { get; set; } = policy;

    public Task<IReadOnlyList<Policy>> SearchAsync(string? query, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Policy>>(Policy is null ? [] : [Policy]);

    public Task<Policy?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(Policy);
}

internal sealed class FakeJobScheduler : IBackgroundJobScheduler
{
    public int EnqueueCount { get; private set; }

    public string Enqueue<TJob>(Expression<Func<TJob, Task>> methodCall)
    {
        EnqueueCount++;
        return $"job-{EnqueueCount}";
    }

    public void AddOrUpdateRecurring<TJob>(string recurringJobId, Expression<Func<TJob, Task>> methodCall, string cronExpression) { }
}

internal sealed class FakeClock(DateTimeOffset now) : IDateTime
{
    public DateTimeOffset UtcNow { get; } = now;
}

internal sealed class FakeAuditLog : IAuditLogService
{
    private readonly List<AuditEntry> _entries = [];

    public IReadOnlyList<AuditEntry> Entries => _entries;
    public int Count(string eventType) => _entries.Count(e => e.EventType == eventType);

    public Task WriteAsync(AuditEntry entry, CancellationToken cancellationToken = default)
    {
        _entries.Add(entry);
        return Task.CompletedTask;
    }

    public Task WriteAndSaveAsync(AuditEntry entry, Guid? actorUserId, CancellationToken cancellationToken = default)
    {
        _entries.Add(entry);
        return Task.CompletedTask;
    }
}

internal sealed class FakeBackgroundJobData(ReserveHistory? txn = null) : IBackgroundJobData
{
    public ReserveHistory? Txn { get; set; } = txn;
    public IReadOnlyList<Claim> StaleClaims { get; set; } = [];
    public DateTimeOffset? LastBreachAt { get; set; }

    public Task<ReserveHistory?> GetReserveTransactionAsync(Guid reserveHistoryId, CancellationToken cancellationToken = default)
        => Task.FromResult(Txn);

    public Task<IReadOnlyList<Claim>> GetStaleClaimsAsync(DateTimeOffset updatedBefore, CancellationToken cancellationToken = default)
        => Task.FromResult(StaleClaims);

    public Task<DateTimeOffset?> GetLastSlaBreachAtAsync(Guid claimId, CancellationToken cancellationToken = default)
        => Task.FromResult(LastBreachAt);
}

internal static class TestData
{
    public static readonly Guid Org = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public static LossEvent NewLossEvent(DateTimeOffset? lossDate = null, string cause = "COL-VEH-COL") => new()
    {
        LossDate = lossDate ?? new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero),
        LossDescription = "Vehicle collision at the north gate causing significant front-end damage.",
        CauseOfLossCode = cause,
        ReportDate = new DateTimeOffset(2026, 5, 2, 0, 0, 0, TimeSpan.Zero),
    };

    public static Claim NewClaim(Guid? policyId = null, DateTimeOffset? lossDate = null) =>
        Claim.Create(Org, "CLM-2026-0000001", policyId, policyId is null ? null : "POL-2024-001001",
            "Acme Logistics", new DateTimeOffset(2026, 5, 2, 0, 0, 0, TimeSpan.Zero), null, null, null,
            NewLossEvent(lossDate));

    public static ClaimParty Claimant() => new()
    {
        PartyRole = PartyRole.Claimant,
        PartyType = PartyType.Person,
        FirstName = "Jane",
        LastName = "Doe",
    };

    /// <summary>Builds a policy-linked claim with one component holding a single pending transaction.</summary>
    public static (Claim Claim, ClaimReserveComponent Component, ReserveHistory Txn) ClaimWithPendingReserve(
        Guid submitter, decimal amount, ReserveComponentType type = ReserveComponentType.Indemnity)
    {
        var claim = NewClaim(policyId: Guid.NewGuid());
        var component = ClaimReserveComponent.Open(Org, claim.Id, type);
        claim.AddReserveComponent(component);
        var txn = component.SubmitTransaction(amount, ReserveTransactionType.Add, "reserve", submitter);
        return (claim, component, txn);
    }

    /// <summary>Flattens a business-rule exception's field-keyed messages for substring assertions.</summary>
    public static string Flatten(this BusinessRuleException ex) => string.Join(" | ", ex.Errors.SelectMany(kv => kv.Value));
}
