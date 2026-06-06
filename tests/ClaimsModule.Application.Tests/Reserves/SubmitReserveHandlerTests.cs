using ClaimsModule.Application.Common.Exceptions;
using ClaimsModule.Application.Reserves;
using ClaimsModule.Application.Tests.TestSupport;
using ClaimsModule.Domain.Entities;
using ClaimsModule.Domain.Enums;
using Xunit;

namespace ClaimsModule.Application.Tests.Reserves;

public class SubmitReserveHandlerTests
{
    [Fact]
    public async Task Reserve_Is_Blocked_When_No_Policy_Linked() // BR-C-06
    {
        var claim = TestData.NewClaim(policyId: null);
        var handler = new SubmitReserveCommandHandler(
            new FakeClaimRepository(claim), new FakeJobScheduler(),
            new FakeCurrentUser { Role = UserRole.Handler }, new FakeUnitOfWork());

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => handler.Handle(
            new SubmitReserveCommand { ClaimId = claim.Id, Component = ReserveComponentType.Indemnity, Amount = 5_000m, ChangeReason = "initial" },
            default));

        Assert.Contains("policy", ex.Flatten());
    }

    [Fact]
    public async Task AutoApproved_Reserve_Enqueues_Gl_And_Records_JobId()
    {
        var claim = TestData.NewClaim(policyId: Guid.NewGuid());
        var jobs = new FakeJobScheduler();
        var handler = new SubmitReserveCommandHandler(
            new FakeClaimRepository(claim), jobs,
            new FakeCurrentUser { Role = UserRole.Handler }, new FakeUnitOfWork());

        var result = await handler.Handle(
            new SubmitReserveCommand { ClaimId = claim.Id, Component = ReserveComponentType.Indemnity, Amount = 5_000m, ChangeReason = "initial" },
            default);

        Assert.True(result.AutoApproved);
        Assert.Equal(1, jobs.EnqueueCount);
        var txn = claim.ReserveComponents.Single().History.Single();
        Assert.False(string.IsNullOrEmpty(txn.PostingJobId));     // FRS §12.1
    }

    [Fact]
    public async Task Pending_Reserve_Does_Not_Enqueue_Gl()
    {
        var claim = TestData.NewClaim(policyId: Guid.NewGuid());
        var jobs = new FakeJobScheduler();
        var handler = new SubmitReserveCommandHandler(
            new FakeClaimRepository(claim), jobs,
            new FakeCurrentUser { Role = UserRole.Handler }, new FakeUnitOfWork());

        var result = await handler.Handle(
            new SubmitReserveCommand { ClaimId = claim.Id, Component = ReserveComponentType.Indemnity, Amount = 50_000m, ChangeReason = "large" },
            default);

        Assert.False(result.AutoApproved);
        Assert.Equal(ReserveApprovalStatus.PendingApproval, result.ApprovalStatus);
        Assert.Equal(0, jobs.EnqueueCount);                       // no GL posting until approval (BR-R-02)
    }

    [Fact]
    public async Task AutoApproved_Crossing_Aggregate_Limit_Requires_Override() // BR-R-05 (auto-approval path)
    {
        var claim = TestData.NewClaim(policyId: Guid.NewGuid());
        var component = ClaimReserveComponent.Open(TestData.Org, claim.Id, ReserveComponentType.Indemnity);
        claim.AddReserveComponent(component);
        var big = component.SubmitTransaction(9_999_000m, ReserveTransactionType.Add, "initial", Guid.NewGuid());
        component.Approve(big, Guid.NewGuid());

        var handler = new SubmitReserveCommandHandler(
            new FakeClaimRepository(claim), new FakeJobScheduler(),
            new FakeCurrentUser { Role = UserRole.Handler }, new FakeUnitOfWork());

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => handler.Handle(
            new SubmitReserveCommand { ClaimId = claim.Id, Component = ReserveComponentType.Indemnity, Amount = 5_000m, ChangeReason = "more" },
            default));

        Assert.Contains("10,000,000", ex.Flatten());
    }
}
