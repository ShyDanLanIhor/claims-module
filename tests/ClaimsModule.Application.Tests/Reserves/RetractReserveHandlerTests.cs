using ClaimsModule.Application.Common.Exceptions;
using ClaimsModule.Application.Reserves;
using ClaimsModule.Application.Tests.TestSupport;
using ClaimsModule.Domain.Enums;
using Xunit;

namespace ClaimsModule.Application.Tests.Reserves;

public class RetractReserveHandlerTests
{
    [Fact]
    public async Task Only_The_Submitter_May_Retract()
    {
        var (claim, _, txn) = TestData.ClaimWithPendingReserve(Guid.NewGuid(), 50_000m);
        var handler = new RetractReserveCommandHandler(
            new FakeClaimRepository(claim),
            new FakeCurrentUser { UserId = Guid.NewGuid() }, // a different user
            new FakeUnitOfWork());

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            handler.Handle(new RetractReserveCommand(claim.Id, txn.Id), default));
    }

    [Fact]
    public async Task Submitter_Retract_Sets_Cancelled()
    {
        var submitter = Guid.NewGuid();
        var (claim, _, txn) = TestData.ClaimWithPendingReserve(submitter, 50_000m);
        var handler = new RetractReserveCommandHandler(
            new FakeClaimRepository(claim),
            new FakeCurrentUser { UserId = submitter },
            new FakeUnitOfWork());

        await handler.Handle(new RetractReserveCommand(claim.Id, txn.Id), default);

        Assert.Equal(ReserveApprovalStatus.Cancelled, txn.ApprovalStatus);
    }
}
