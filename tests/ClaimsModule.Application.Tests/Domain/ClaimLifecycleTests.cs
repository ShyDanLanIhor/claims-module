using ClaimsModule.Domain.Claims;
using ClaimsModule.Domain.Enums;
using Xunit;

namespace ClaimsModule.Application.Tests.Domain;

public class ClaimLifecycleTests
{
    [Theory]
    [InlineData(ClaimStatus.Draft, ClaimStatus.Open)]
    [InlineData(ClaimStatus.Open, ClaimStatus.UnderInvestigation)]
    [InlineData(ClaimStatus.Open, ClaimStatus.PendingPayment)]
    [InlineData(ClaimStatus.PendingPayment, ClaimStatus.Closed)]
    [InlineData(ClaimStatus.Closed, ClaimStatus.Reopened)]
    public void Allowed_Transitions_Are_Permitted(ClaimStatus from, ClaimStatus to) =>
        Assert.True(ClaimLifecycle.IsAllowed(from, to));

    [Theory]
    [InlineData(ClaimStatus.Draft, ClaimStatus.Closed)] // BR-C-06: no direct Draft → Closed
    [InlineData(ClaimStatus.Draft, ClaimStatus.PendingPayment)]
    [InlineData(ClaimStatus.Closed, ClaimStatus.Open)]
    [InlineData(ClaimStatus.Withdrawn, ClaimStatus.Open)]
    public void Disallowed_Transitions_Are_Rejected(ClaimStatus from, ClaimStatus to) =>
        Assert.False(ClaimLifecycle.IsAllowed(from, to));

    [Fact]
    public void Reopen_Requires_Supervisor_Role()
    {
        var role = ClaimLifecycle.RequiredRole(ClaimStatus.Closed, ClaimStatus.Reopened);
        Assert.Equal(UserRole.Supervisor, role);
    }

    [Fact]
    public void AllowedNext_From_Open_Lists_Expected_Targets()
    {
        var next = ClaimLifecycle.AllowedNext(ClaimStatus.Open);
        Assert.Contains(ClaimStatus.UnderInvestigation, next);
        Assert.Contains(ClaimStatus.PendingPayment, next);
        Assert.Contains(ClaimStatus.Closed, next);
        Assert.Contains(ClaimStatus.Withdrawn, next);
        Assert.DoesNotContain(ClaimStatus.Reopened, next);
    }
}
