using ClaimsModule.Domain.Entities;
using ClaimsModule.Domain.Enums;
using Xunit;

namespace ClaimsModule.Application.Tests.Domain;

public class ClaimClosureRuleTests
{
    private static Claim ClaimWith(params (ReserveComponentType Component, decimal Amount)[] reserves)
    {
        var org = Guid.NewGuid();
        var claim = Claim.Create(org, "CLM-2026-0000001", Guid.NewGuid(), "POL-2024-001001", "Client",
            DateTimeOffset.UtcNow, null, null, null,
            new LossEvent { LossDate = DateTimeOffset.UtcNow, LossDescription = "x", CauseOfLossCode = "COL-FIRE", ReportDate = DateTimeOffset.UtcNow });

        foreach (var (component, amount) in reserves)
        {
            var rc = claim.AddReserveComponent(ClaimReserveComponent.Open(org, claim.Id, component));
            rc.SubmitTransaction(amount, ReserveTransactionType.Add, "seed", Guid.NewGuid());
        }

        return claim;
    }

    [Fact]
    public void HasOpenReserve_Detects_Positive_Component_Even_When_Aggregate_Is_Negative() // CC-04 regression
    {
        var claim = ClaimWith(
            (ReserveComponentType.Indemnity, 5_000m),
            (ReserveComponentType.SubrogationRecoverable, -8_000m));

        Assert.True(claim.TotalApprovedReserves < 0);  // aggregate masks the open reserve
        Assert.True(claim.HasOpenReserve);             // per-component check still sees the $5k Indemnity
    }

    [Fact]
    public void HasOpenReserve_Is_False_When_All_Components_Are_Zero_Or_Negative()
    {
        var claim = ClaimWith((ReserveComponentType.SubrogationRecoverable, -2_000m));
        Assert.False(claim.HasOpenReserve);
    }
}
