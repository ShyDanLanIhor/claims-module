using AutoMapper;
using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Domain.Claims;
using ClaimsModule.Domain.Documents;
using ClaimsModule.Domain.Enums;
using MediatR;

namespace ClaimsModule.Application.Reference;

/// <summary>Lists active cause-of-loss codes, optionally filtered by peril category
/// (FRS §10.3 GET /api/reference/cause-of-loss-codes).</summary>
public sealed record ListCauseOfLossCodesQuery(PerilCategory? PerilCategory) : IRequest<IReadOnlyList<CauseOfLossCodeDto>>;

public sealed class ListCauseOfLossCodesQueryHandler(IReferenceDataRepository reference, IMapper mapper)
    : IRequestHandler<ListCauseOfLossCodesQuery, IReadOnlyList<CauseOfLossCodeDto>>
{
    public async Task<IReadOnlyList<CauseOfLossCodeDto>> Handle(ListCauseOfLossCodesQuery request, CancellationToken cancellationToken)
    {
        var codes = await reference.GetCauseOfLossCodesAsync(request.PerilCategory, activeOnly: true, cancellationToken);
        return mapper.Map<List<CauseOfLossCodeDto>>(codes);
    }
}

/// <summary>Lists all claim statuses and their valid next-statuses, derived from the lifecycle state
/// machine (FRS §10.3 GET /api/reference/claim-statuses).</summary>
public sealed record ListClaimStatusesQuery : IRequest<IReadOnlyList<ClaimStatusInfoDto>>;

public sealed class ListClaimStatusesQueryHandler : IRequestHandler<ListClaimStatusesQuery, IReadOnlyList<ClaimStatusInfoDto>>
{
    public Task<IReadOnlyList<ClaimStatusInfoDto>> Handle(ListClaimStatusesQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyList<ClaimStatusInfoDto> result = Enum.GetValues<ClaimStatus>()
            .Select(s => new ClaimStatusInfoDto
            {
                Status = s,
                AllowedNextStatuses = ClaimLifecycle.AllowedNext(s)
            })
            .ToList();

        return Task.FromResult(result);
    }
}

/// <summary>Lists the accepted document categories for the upload picker
/// (GET /api/reference/document-types). The single source of truth is Domain <c>DocumentTypes</c>,
/// which the upload validator also uses — so the Angular picker never hardcodes the list.</summary>
public sealed record ListDocumentTypesQuery : IRequest<IReadOnlyList<string>>;

public sealed class ListDocumentTypesQueryHandler : IRequestHandler<ListDocumentTypesQuery, IReadOnlyList<string>>
{
    public Task<IReadOnlyList<string>> Handle(ListDocumentTypesQuery request, CancellationToken cancellationToken) =>
        Task.FromResult(DocumentTypes.All);
}
