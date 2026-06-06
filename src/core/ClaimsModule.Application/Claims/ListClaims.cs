using AutoMapper;
using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Application.Common.Models;
using ClaimsModule.Domain.Enums;
using MediatR;

namespace ClaimsModule.Application.Claims;

/// <summary>Paginated, filterable claims list (FRS §10.1 GET /api/claims, §11.1 dashboard).</summary>
public sealed record ListClaimsQuery : IRequest<PagedResult<ClaimSummaryDto>>
{
    public IReadOnlyList<ClaimStatus>? Statuses { get; init; }
    public DateTimeOffset? LossDateFrom { get; init; }
    public DateTimeOffset? LossDateTo { get; init; }
    public Guid? AssignedHandlerId { get; init; }
    public string? CauseOfLossCode { get; init; }
    public Guid? PolicyId { get; init; }
    public string? Search { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public sealed class ListClaimsQueryHandler(IClaimReadRepository claims, IMapper mapper)
    : IRequestHandler<ListClaimsQuery, PagedResult<ClaimSummaryDto>>
{
    public async Task<PagedResult<ClaimSummaryDto>> Handle(ListClaimsQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);

        var filter = new ClaimListFilter
        {
            Statuses = request.Statuses,
            LossDateFrom = request.LossDateFrom,
            LossDateTo = request.LossDateTo,
            AssignedHandlerId = request.AssignedHandlerId,
            CauseOfLossCode = request.CauseOfLossCode,
            PolicyId = request.PolicyId,
            Search = request.Search,
            Page = page,
            PageSize = pageSize
        };

        var (items, total) = await claims.ListAsync(filter, cancellationToken);
        var dtos = mapper.Map<List<ClaimSummaryDto>>(items);
        return new PagedResult<ClaimSummaryDto>(dtos, total, page, pageSize);
    }
}
