using ClaimsModule.Application.Claims;
using ClaimsModule.Application.Common.Exceptions;
using ClaimsModule.Application.Tests.TestSupport;
using ClaimsModule.Domain.Entities;
using ClaimsModule.Domain.Enums;
using Xunit;

namespace ClaimsModule.Application.Tests.Claims;

public class ChangeClaimStatusHandlerTests
{
    private static ChangeClaimStatusCommandHandler Handler(Claim claim, UserRole role, Policy? policy = null) =>
        new(new FakeClaimRepository(claim), new FakePolicyRepository(policy),
            new FakeCurrentUser { Role = role }, new FakeUnitOfWork());

    [Fact]
    public async Task Invalid_Transition_Returns_422_With_Valid_Next() // BR-ST-01
    {
        var claim = TestData.NewClaim(); // Draft
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => Handler(claim, UserRole.Manager).Handle(
            new ChangeClaimStatusCommand { ClaimId = claim.Id, TargetStatus = ClaimStatus.Closed }, default));

        Assert.Contains("not permitted", ex.Flatten());
    }

    [Fact]
    public async Task Open_Requires_At_Least_One_Claimant() // BR-ST-02
    {
        var claim = TestData.NewClaim(); // Draft, no parties, no policy
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => Handler(claim, UserRole.Handler).Handle(
            new ChangeClaimStatusCommand { ClaimId = claim.Id, TargetStatus = ClaimStatus.Open }, default));

        Assert.Contains("Claimant", ex.Flatten());
    }

    [Fact]
    public async Task Open_Outside_Policy_Period_Requires_Acknowledgement() // BR-C-02
    {
        var (claim, policy) = ClaimOutsidePolicyPeriod();

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => Handler(claim, UserRole.Handler, policy).Handle(
            new ChangeClaimStatusCommand { ClaimId = claim.Id, TargetStatus = ClaimStatus.Open, AcknowledgeWarnings = false }, default));

        Assert.True(ex.Errors.ContainsKey("acknowledgeWarnings"));
    }

    [Fact]
    public async Task Open_Outside_Policy_Period_Succeeds_When_Acknowledged() // BR-C-02
    {
        var (claim, policy) = ClaimOutsidePolicyPeriod();

        await Handler(claim, UserRole.Handler, policy).Handle(
            new ChangeClaimStatusCommand { ClaimId = claim.Id, TargetStatus = ClaimStatus.Open, AcknowledgeWarnings = true }, default);

        Assert.Equal(ClaimStatus.Open, claim.Status);
    }

    [Fact]
    public async Task Close_Is_Blocked_By_A_Pending_Reserve() // CC-01
    {
        var claim = OpenClaimWithClaimant();
        var component = ClaimReserveComponent.Open(TestData.Org, claim.Id, ReserveComponentType.Indemnity);
        claim.AddReserveComponent(component);
        component.SubmitTransaction(50_000m, ReserveTransactionType.Add, "pending", Guid.NewGuid()); // PendingApproval

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => Handler(claim, UserRole.Manager).Handle(
            new ChangeClaimStatusCommand { ClaimId = claim.Id, TargetStatus = ClaimStatus.Closed, Reason = "done" }, default));

        Assert.Contains("PendingApproval", ex.Flatten());
    }

    [Fact]
    public async Task Close_With_Open_Reserve_Requires_A_Justification() // CC-04
    {
        var claim = OpenClaimWithApprovedReserve();

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => Handler(claim, UserRole.Manager).Handle(
            new ChangeClaimStatusCommand { ClaimId = claim.Id, TargetStatus = ClaimStatus.Closed, Reason = null }, default));

        Assert.Contains("justification", ex.Flatten());
    }

    [Fact]
    public async Task Close_Succeeds_With_Justification_When_A_Reserve_Is_Open() // CC-04 (satisfied)
    {
        var claim = OpenClaimWithApprovedReserve();

        await Handler(claim, UserRole.Manager).Handle(
            new ChangeClaimStatusCommand { ClaimId = claim.Id, TargetStatus = ClaimStatus.Closed, Reason = "Settled in full." }, default);

        Assert.Equal(ClaimStatus.Closed, claim.Status);
    }

    private static (Claim Claim, Policy Policy) ClaimOutsidePolicyPeriod()
    {
        var claim = TestData.NewClaim(policyId: Guid.NewGuid(), lossDate: new DateTimeOffset(2023, 6, 1, 0, 0, 0, TimeSpan.Zero));
        claim.AddParty(TestData.Claimant());
        var policy = new Policy { EffectiveDate = new DateOnly(2024, 1, 1), ExpirationDate = new DateOnly(2026, 12, 31) };
        return (claim, policy);
    }

    private static Claim OpenClaimWithClaimant()
    {
        var claim = TestData.NewClaim(policyId: Guid.NewGuid());
        claim.AddParty(TestData.Claimant());
        claim.ChangeStatus(ClaimStatus.Open, null); // Draft → Open (allowed transition)
        return claim;
    }

    private static Claim OpenClaimWithApprovedReserve()
    {
        var claim = OpenClaimWithClaimant();
        var component = ClaimReserveComponent.Open(TestData.Org, claim.Id, ReserveComponentType.Indemnity);
        claim.AddReserveComponent(component);
        component.SubmitTransaction(5_000m, ReserveTransactionType.Add, "auto", Guid.NewGuid()); // auto-approved → positive balance
        return claim;
    }
}
