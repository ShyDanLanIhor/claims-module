using ClaimsModule.Application.Common.Exceptions;
using ClaimsModule.Application.Reserves;
using ClaimsModule.Application.Tests.TestSupport;
using ClaimsModule.Domain.Entities;
using ClaimsModule.Domain.Enums;
using Xunit;

namespace ClaimsModule.Application.Tests.Reserves;

public class ApproveReserveHandlerTests
{
    [Fact]
    public async Task Self_Approval_Is_Rejected() // BR-R-03
    {
        var user = Guid.NewGuid();
        var (claim, _, txn) = TestData.ClaimWithPendingReserve(user, 50_000m);
        var handler = new ApproveReserveCommandHandler(
            new FakeClaimRepository(claim), new FakeJobScheduler(),
            new FakeCurrentUser { UserId = user, Role = UserRole.Supervisor }, new FakeUnitOfWork());

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            handler.Handle(new ApproveReserveCommand { ClaimId = claim.Id, TransactionId = txn.Id }, default));
        Assert.Contains("Self-approval", ex.Flatten());
    }

    [Fact]
    public async Task Handler_Role_Cannot_Approve_Above_Authority() // BR-R-02
    {
        var (claim, _, txn) = TestData.ClaimWithPendingReserve(Guid.NewGuid(), 50_000m);
        var handler = new ApproveReserveCommandHandler(
            new FakeClaimRepository(claim), new FakeJobScheduler(),
            new FakeCurrentUser { Role = UserRole.Handler }, new FakeUnitOfWork());

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            handler.Handle(new ApproveReserveCommand { ClaimId = claim.Id, TransactionId = txn.Id }, default));
        Assert.Contains("authority", ex.Flatten());
    }

    [Fact]
    public async Task Supervisor_Approval_Enqueues_Gl_And_Records_JobId()
    {
        var (claim, component, txn) = TestData.ClaimWithPendingReserve(Guid.NewGuid(), 50_000m);
        var jobs = new FakeJobScheduler();
        var handler = new ApproveReserveCommandHandler(
            new FakeClaimRepository(claim), jobs,
            new FakeCurrentUser { Role = UserRole.Supervisor }, new FakeUnitOfWork());

        await handler.Handle(new ApproveReserveCommand { ClaimId = claim.Id, TransactionId = txn.Id }, default);

        Assert.Equal(ReserveApprovalStatus.Approved, txn.ApprovalStatus);
        Assert.Equal(50_000m, component.CurrentAmount);
        Assert.Equal(1, jobs.EnqueueCount);                       // GL posting enqueued (FRS §6.5)
        Assert.False(string.IsNullOrEmpty(txn.PostingJobId));     // real Hangfire job id recorded (FRS §12.1)
    }

    [Fact]
    public async Task Aggregate_Over_Limit_Without_Override_Is_Blocked() // BR-R-05
    {
        var (claim, txn) = ClaimNearAggregateLimit();
        var handler = new ApproveReserveCommandHandler(
            new FakeClaimRepository(claim), new FakeJobScheduler(),
            new FakeCurrentUser { Role = UserRole.Manager }, new FakeUnitOfWork());

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => handler.Handle(
            new ApproveReserveCommand { ClaimId = claim.Id, TransactionId = txn.Id, ApplyManagerOverride = false }, default));

        Assert.Contains("10,000,000", ex.Flatten());
        Assert.False(claim.ManagerOverrideApplied);
    }

    [Fact]
    public async Task Manager_Override_Permits_Crossing_The_Aggregate_Limit() // BR-R-05
    {
        var (claim, txn) = ClaimNearAggregateLimit();
        var handler = new ApproveReserveCommandHandler(
            new FakeClaimRepository(claim), new FakeJobScheduler(),
            new FakeCurrentUser { Role = UserRole.Manager }, new FakeUnitOfWork());

        await handler.Handle(
            new ApproveReserveCommand { ClaimId = claim.Id, TransactionId = txn.Id, ApplyManagerOverride = true }, default);

        Assert.True(claim.ManagerOverrideApplied);
        Assert.Equal(ReserveApprovalStatus.Approved, txn.ApprovalStatus);
    }

    /// <summary>A claim with $9,999,000 already approved and a fresh $50,000 pending transaction
    /// whose approval would push the aggregate past the $10M limit.</summary>
    private static (Claim Claim, ReserveHistory Txn) ClaimNearAggregateLimit()
    {
        var claim = TestData.NewClaim(policyId: Guid.NewGuid());
        var component = ClaimReserveComponent.Open(TestData.Org, claim.Id, ReserveComponentType.Indemnity);
        claim.AddReserveComponent(component);
        var big = component.SubmitTransaction(9_999_000m, ReserveTransactionType.Add, "initial", Guid.NewGuid());
        component.Approve(big, Guid.NewGuid());
        var txn = component.SubmitTransaction(50_000m, ReserveTransactionType.Add, "more", Guid.NewGuid());
        return (claim, txn);
    }
}
