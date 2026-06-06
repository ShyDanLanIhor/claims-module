using ClaimsModule.Application.Tests.TestSupport;
using ClaimsModule.Domain.Auditing;
using ClaimsModule.Domain.Entities;
using ClaimsModule.Domain.Enums;
using ClaimsModule.Infrastructure.Jobs;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ClaimsModule.Application.Tests.Jobs;

public class PostGlReserveChangeJobTests
{
    private static PostGlReserveChangeJob Build(ReserveHistory txn, FakeAuditLog audit) =>
        new(new FakeBackgroundJobData(txn), audit, new FakeUnitOfWork(), NullLogger<PostGlReserveChangeJob>.Instance);

    private static ReserveHistory AutoApprovedTxn(decimal amount = 5_000m)
    {
        var component = ClaimReserveComponent.Open(TestData.Org, Guid.NewGuid(), ReserveComponentType.Indemnity);
        var txn = component.SubmitTransaction(amount, ReserveTransactionType.Add, "initial", Guid.NewGuid());
        txn.ReserveComponent = component; // EF populates this via Include in production; set it for the fake path
        return txn;
    }

    [Fact]
    public async Task Posts_A_Single_Gl_Entry_And_Marks_Posted()
    {
        var txn = AutoApprovedTxn();
        var audit = new FakeAuditLog();

        await Build(txn, audit).PostAsync(txn.Id);

        Assert.Equal(PostingStatus.Posted, txn.PostingStatus);
        Assert.Equal(1, audit.Count(AuditEventTypes.GlPostingSimulated));
    }

    [Fact]
    public async Task Running_Twice_Produces_No_Duplicate_Audit() // BR-R-06 / FRS §12.1 re-entrancy
    {
        var txn = AutoApprovedTxn();
        var audit = new FakeAuditLog();
        var job = Build(txn, audit);

        await job.PostAsync(txn.Id);
        await job.PostAsync(txn.Id); // already Posted → idempotent no-op

        Assert.Equal(1, audit.Count(AuditEventTypes.GlPostingSimulated));
    }

    [Fact]
    public async Task Skips_A_Pending_Transaction()
    {
        var component = ClaimReserveComponent.Open(TestData.Org, Guid.NewGuid(), ReserveComponentType.Indemnity);
        var txn = component.SubmitTransaction(50_000m, ReserveTransactionType.Add, "pending", Guid.NewGuid()); // not effective
        txn.ReserveComponent = component;
        var audit = new FakeAuditLog();

        await Build(txn, audit).PostAsync(txn.Id);

        Assert.Equal(0, audit.Count(AuditEventTypes.GlPostingSimulated));
        Assert.NotEqual(PostingStatus.Posted, txn.PostingStatus);
    }
}
