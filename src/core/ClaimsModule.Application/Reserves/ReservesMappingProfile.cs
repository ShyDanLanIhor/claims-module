using AutoMapper;
using ClaimsModule.Domain.Entities;

namespace ClaimsModule.Application.Reserves;

public sealed class ReservesMappingProfile : Profile
{
    public ReservesMappingProfile()
    {
        CreateMap<ClaimReserveComponent, ReserveComponentSummaryDto>();

        // ReserveTransactionDto is projected by hand in GetClaimReserves (Component is taken from the
        // owning component in that query), so no ReserveHistory -> ReserveTransactionDto map is needed.
    }
}
