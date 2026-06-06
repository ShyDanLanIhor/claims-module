using AutoMapper;
using ClaimsModule.Application.Common.Exceptions;
using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Domain.Claims;
using MediatR;

namespace ClaimsModule.Application.Claims;

/// <summary>Full claim detail incl. parties, risk objects, reserves, documents and recent audit
/// (FRS §10.1 GET /api/claims/{id}).</summary>
public sealed record GetClaimDetailQuery(Guid ClaimId) : IRequest<ClaimDetailDto>;

public sealed class GetClaimDetailQueryHandler(IClaimReadRepository claims, IMapper mapper)
    : IRequestHandler<GetClaimDetailQuery, ClaimDetailDto>
{
    private const int RecentAuditCount = 10;

    public async Task<ClaimDetailDto> Handle(GetClaimDetailQuery request, CancellationToken cancellationToken)
    {
        var claim = await claims.GetDetailAsync(request.ClaimId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Claim), request.ClaimId);

        var dto = mapper.Map<ClaimDetailDto>(claim);

        var (auditEntries, _) = await claims.GetAuditLogAsync(request.ClaimId, 1, RecentAuditCount, cancellationToken);

        return dto with
        {
            AllowedNextStatuses = ClaimLifecycle.AllowedNext(claim.Status),
            RecentAudit = mapper.Map<List<AuditLogEntryDto>>(auditEntries)
        };
    }
}
