using ClaimsModule.Application.Common.Exceptions;
using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Domain.Entities;
using ClaimsModule.Domain.Enums;

namespace ClaimsModule.Application.Reserves;

/// <summary>
/// BR-R-05 aggregate-override gate, shared by SubmitReserve and ApproveReserve so the rule is
/// evaluated and enforced in exactly one place (the threshold itself lives on the Claim aggregate).
/// </summary>
internal static class ReserveOverride
{
    public static void Ensure(Claim claim, decimal amount, ICurrentUserService currentUser, bool applyManagerOverride)
    {
        if (!claim.WouldExceedAggregateLimit(amount))
            return;

        if (currentUser.Role == UserRole.Manager && applyManagerOverride)
            claim.ApplyManagerOverride();
        else
            throw new BusinessRuleException("override",
                "Total reserves will exceed $10,000,000. A manager override is required."); // BR-R-05
    }
}
