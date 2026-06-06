using AutoMapper;
using ClaimsModule.Application.Reserves;
using ClaimsModule.Domain.Entities;

namespace ClaimsModule.Application.Claims;

/// <summary>Entity → DTO maps for the Claims feature (AutoMapper profiles live in the Application layer).</summary>
public sealed class ClaimsMappingProfile : Profile
{
    public ClaimsMappingProfile()
    {
        CreateMap<LossEvent, LossEventDto>();
        CreateMap<ClaimParty, ClaimPartyDto>();
        CreateMap<ClaimRiskObject, ClaimRiskObjectDto>();
        CreateMap<ClaimDocument, ClaimDocumentMetadataDto>();
        CreateMap<ClaimAuditLog, AuditLogEntryDto>();

        CreateMap<Claim, ClaimSummaryDto>()
            .ForMember(d => d.LossDate, o => o.MapFrom(s => s.LossEvent.LossDate))
            .ForMember(d => d.CauseOfLossCode, o => o.MapFrom(s => s.LossEvent.CauseOfLossCode))
            .ForMember(d => d.TotalReserves, o => o.MapFrom(s => s.ReserveComponents.Sum(c => c.CurrentAmount)));

        CreateMap<Claim, ClaimDetailDto>()
            .ForMember(d => d.Reserves, o => o.MapFrom(s => s.ReserveComponents))
            .ForMember(d => d.Documents, o => o.MapFrom(s => s.Documents))
            .ForMember(d => d.AllowedNextStatuses, o => o.Ignore())
            .ForMember(d => d.RecentAudit, o => o.Ignore());
    }
}
