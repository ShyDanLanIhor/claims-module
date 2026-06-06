using ClaimsModule.Application.Claims;
using ClaimsModule.Application.Common.Exceptions;
using ClaimsModule.Application.Tests.TestSupport;
using ClaimsModule.Domain.Entities;
using ClaimsModule.Domain.Enums;
using Xunit;

namespace ClaimsModule.Application.Tests.Claims;

public class ManagePartiesHandlerTests
{
    [Fact]
    public async Task Cannot_Remove_The_Last_Claimant() // FRS §10.1 DELETE party
    {
        var claim = TestData.NewClaim(policyId: Guid.NewGuid());
        var claimant = claim.AddParty(TestData.Claimant());
        var handler = new RemoveClaimPartyCommandHandler(new FakeClaimRepository(claim), new FakeUnitOfWork());

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            handler.Handle(new RemoveClaimPartyCommand(claim.Id, claimant.Id), default));

        Assert.Contains("Claimant", ex.Flatten());
    }

    [Fact]
    public async Task A_NonLast_Party_Can_Be_Soft_Removed()
    {
        var claim = TestData.NewClaim(policyId: Guid.NewGuid());
        claim.AddParty(TestData.Claimant());
        var witness = claim.AddParty(new ClaimParty
        {
            PartyRole = PartyRole.Witness,
            PartyType = PartyType.Person,
            FirstName = "Will",
            LastName = "Ness",
        });
        var handler = new RemoveClaimPartyCommandHandler(new FakeClaimRepository(claim), new FakeUnitOfWork());

        await handler.Handle(new RemoveClaimPartyCommand(claim.Id, witness.Id), default);

        Assert.False(witness.IsActive);
    }

    [Fact]
    public async Task A_Second_Claimant_Allows_Removing_The_First()
    {
        var claim = TestData.NewClaim(policyId: Guid.NewGuid());
        var first = claim.AddParty(TestData.Claimant());
        claim.AddParty(TestData.Claimant()); // a second claimant remains
        var handler = new RemoveClaimPartyCommandHandler(new FakeClaimRepository(claim), new FakeUnitOfWork());

        await handler.Handle(new RemoveClaimPartyCommand(claim.Id, first.Id), default);

        Assert.False(first.IsActive);
    }
}
