using AutoMapper;
using ClaimsModule.Domain.Entities;

namespace ClaimsModule.Application.Policies;

public sealed class PoliciesMappingProfile : Profile
{
    public PoliciesMappingProfile()
    {
        CreateMap<Policy, PolicyDto>()
            .ForMember(d => d.CoverageTypes, o => o.MapFrom(s =>
                s.CoverageTypes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)));
    }
}
