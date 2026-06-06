using AutoMapper;
using ClaimsModule.Application.Common.Exceptions;
using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Application.Common.Models;
using MediatR;

namespace ClaimsModule.Application.Claims;

/// <summary>Paginated, reverse-chronological audit log for a claim (FRS §10.1 GET /api/claims/{id}/audit).</summary>
public sealed record GetClaimAuditLogQuery(Guid ClaimId, int Page = 1, int PageSize = 25)
    : IRequest<PagedResult<AuditLogEntryDto>>;

public sealed class GetClaimAuditLogQueryHandler(IClaimReadRepository claims, IMapper mapper)
    : IRequestHandler<GetClaimAuditLogQuery, PagedResult<AuditLogEntryDto>>
{
    public async Task<PagedResult<AuditLogEntryDto>> Handle(GetClaimAuditLogQuery request, CancellationToken cancellationToken)
    {
        if (!await claims.ExistsAsync(request.ClaimId, cancellationToken))
            throw new NotFoundException(nameof(Domain.Entities.Claim), request.ClaimId);

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);

        var (items, total) = await claims.GetAuditLogAsync(request.ClaimId, page, pageSize, cancellationToken);
        return new PagedResult<AuditLogEntryDto>(mapper.Map<List<AuditLogEntryDto>>(items), total, page, pageSize);
    }
}
