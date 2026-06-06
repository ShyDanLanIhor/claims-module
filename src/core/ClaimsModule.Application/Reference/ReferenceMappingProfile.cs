using AutoMapper;
using ClaimsModule.Domain.Entities;

namespace ClaimsModule.Application.Reference;

public sealed class ReferenceMappingProfile : Profile
{
    public ReferenceMappingProfile()
    {
        CreateMap<CauseOfLossCode, CauseOfLossCodeDto>();
    }
}
