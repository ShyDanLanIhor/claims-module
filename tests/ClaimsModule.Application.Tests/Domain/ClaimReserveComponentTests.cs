using ClaimsModule.Domain.Common;
using ClaimsModule.Domain.Entities;
using ClaimsModule.Domain.Enums;
using ClaimsModule.Domain.Events;
using Xunit;

namespace ClaimsModule.Application.Tests.Domain;

public class ClaimReserveComponentTests
{
    private static ClaimReserveComponent NewComponent() =>
        ClaimReserveComponent.Open(Guid.NewGuid(), Guid.NewGuid(), ReserveComponentType.Indemnity);

    [Fact]
    public void SubmitTransaction_Within_AutoApproval_Sets_Balance_And_AutoApproves()
    {
        var component = NewComponent();
        var user = Guid.NewGuid();

        var txn = component.SubmitTransaction(5_000m, ReserveTransactionType.Add, "initial", user);

        Assert.Equal(ReserveApprovalStatus.AutoApproved, txn.ApprovalStatus);
        Assert.Equal(5_000m, component.CurrentAmount);
        Assert.Equal(1, txn.ChangeSequence);
        Assert.Equal($"Reserve:{component.Id}:Change:1", txn.IdempotencyKey);

        var submitted = Assert.IsType<ReserveSubmittedDomainEvent>(Assert.Single(component.DomainEvents));
        Assert.True(submitted.AutoApproved);
    }

    [Fact]
    public void SubmitTransaction_Above_AutoApproval_Is_Pending_And_Does_Not_Move_Balance()
    {
        var component = NewComponent();

        var txn = component.SubmitTransaction(50_000m, ReserveTransactionType.Add, "reserve", Guid.NewGuid());

        Assert.Equal(ReserveApprovalStatus.PendingApproval, txn.ApprovalStatus);
        Assert.Equal(0m, component.CurrentAmount);          // pending does not count toward balance
        Assert.Equal(50_000m, component.PendingAmount);
    }

    [Fact]
    public void Approve_Moves_Balance_And_Records_Approver()
    {
        var component = NewComponent();
        var submitter = Guid.NewGuid();
        var approver = Guid.NewGuid();
        var txn = component.SubmitTransaction(50_000m, ReserveTransactionType.Add, "reserve", submitter);

        component.Approve(txn, approver);

        Assert.Equal(ReserveApprovalStatus.Approved, txn.ApprovalStatus);
        Assert.Equal(approver, txn.ApprovedByUserId);
        Assert.Equal(50_000m, component.CurrentAmount);
        Assert.Contains(component.DomainEvents, e => e is ReserveApprovedDomainEvent);
    }

    [Fact]
    public void Reject_Retains_Transaction_And_Cancels_Posting()
    {
        var component = NewComponent();
        var txn = component.SubmitTransaction(50_000m, ReserveTransactionType.Add, "reserve", Guid.NewGuid());

        component.Reject(txn, Guid.NewGuid(), "insufficient documentation");

        Assert.Equal(ReserveApprovalStatus.Rejected, txn.ApprovalStatus);
        Assert.Equal(PostingStatus.Cancelled, txn.PostingStatus);
        Assert.Equal(0m, component.CurrentAmount);
        Assert.Single(component.History); // rejected record is retained (BR-R-04)
    }

    [Fact]
    public void RetryGlPosting_On_Failed_Resets_To_Pending_And_Raises_Event()
    {
        var component = NewComponent();
        var user = Guid.NewGuid();
        var txn = component.SubmitTransaction(5_000m, ReserveTransactionType.Add, "initial", user);
        component.MarkPostingFailed(txn);
        Assert.Equal(PostingStatus.Failed, txn.PostingStatus);

        component.RetryGlPosting(txn, user);

        Assert.Equal(PostingStatus.Pending, txn.PostingStatus); // re-queued for the idempotent posting job
        Assert.Equal(5_000m, component.CurrentAmount);          // balance unaffected by a retry
        Assert.Contains(component.DomainEvents, e => e is ReserveGlRetryRequestedDomainEvent);
    }

    [Fact]
    public void RetryGlPosting_When_Not_Failed_Throws()
    {
        var component = NewComponent();
        var txn = component.SubmitTransaction(5_000m, ReserveTransactionType.Add, "initial", Guid.NewGuid());
        // txn.PostingStatus is Pending, never Failed.

        Assert.Throws<DomainException>(() => component.RetryGlPosting(txn, Guid.NewGuid()));
    }

    [Fact]
    public void ChangeSequence_Increments_Per_Transaction()
    {
        var component = NewComponent();

        var first = component.SubmitTransaction(1_000m, ReserveTransactionType.Add, "a", Guid.NewGuid());
        var second = component.SubmitTransaction(2_000m, ReserveTransactionType.Add, "b", Guid.NewGuid());

        Assert.Equal(1, first.ChangeSequence);
        Assert.Equal(2, second.ChangeSequence);
        Assert.Equal(3_000m, component.CurrentAmount);
    }
}
