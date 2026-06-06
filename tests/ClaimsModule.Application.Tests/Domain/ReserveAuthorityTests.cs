using ClaimsModule.Domain.Enums;
using ClaimsModule.Domain.Reserves;
using Xunit;

namespace ClaimsModule.Application.Tests.Domain;

public class ReserveAuthorityTests
{
    [Theory]
    [InlineData(0.01)]
    [InlineData(5_000)]
    [InlineData(10_000)] // boundary: ≤ $10k auto-approves
    public void Amounts_At_Or_Below_AutoApprovalLimit_Are_AutoApproved(decimal amount)
    {
        Assert.True(ReserveAuthority.IsAutoApproved(amount));
        Assert.Equal(ReserveApprovalStatus.AutoApproved, ReserveAuthority.InitialApprovalStatus(amount));
        Assert.Equal(UserRole.Handler, ReserveAuthority.RequiredRole(amount));
    }

    [Theory]
    [InlineData(10_000.01)]
    [InlineData(100_000)] // boundary: ≤ $100k needs Supervisor
    public void Amounts_Above_Auto_Through_SupervisorLimit_Require_Supervisor(decimal amount)
    {
        Assert.False(ReserveAuthority.IsAutoApproved(amount));
        Assert.Equal(ReserveApprovalStatus.PendingApproval, ReserveAuthority.InitialApprovalStatus(amount));
        Assert.Equal(UserRole.Supervisor, ReserveAuthority.RequiredRole(amount));
        Assert.True(ReserveAuthority.CanApprove(UserRole.Supervisor, amount));
        Assert.True(ReserveAuthority.CanApprove(UserRole.Manager, amount));
        Assert.False(ReserveAuthority.CanApprove(UserRole.Handler, amount));
    }

    [Theory]
    [InlineData(100_000.01)]
    [InlineData(5_000_000)]
    public void Amounts_Above_SupervisorLimit_Require_Manager(decimal amount)
    {
        Assert.Equal(UserRole.Manager, ReserveAuthority.RequiredRole(amount));
        Assert.False(ReserveAuthority.CanApprove(UserRole.Supervisor, amount));
        Assert.True(ReserveAuthority.CanApprove(UserRole.Manager, amount));
    }

    [Fact]
    public void Subrogation_Negative_Amount_Is_Tiered_By_Magnitude()
    {
        // A −$50,000 subrogation recoverable is a $50k-magnitude transaction → Supervisor tier.
        Assert.Equal(UserRole.Supervisor, ReserveAuthority.RequiredRole(-50_000));
        Assert.False(ReserveAuthority.IsAutoApproved(-50_000));
        Assert.True(ReserveAuthority.IsAutoApproved(-9_999));
    }
}
