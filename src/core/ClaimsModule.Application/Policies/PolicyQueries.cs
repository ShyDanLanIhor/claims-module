using AutoMapper;
using ClaimsModule.Application.Common.Exceptions;
using ClaimsModule.Application.Common.Interfaces;
using MediatR;

namespace ClaimsModule.Application.Policies;

/// <summary>Searches the simulated policy dataset (FRS §10.3 GET /api/policies/search).</summary>
public sealed record SearchPoliciesQuery(string? Query) : IRequest<IReadOnlyList<PolicyDto>>;

public sealed class SearchPoliciesQueryHandler(IPolicyRepository policies, IMapper mapper)
    : IRequestHandler<SearchPoliciesQuery, IReadOnlyList<PolicyDto>>
{
    public async Task<IReadOnlyList<PolicyDto>> Handle(SearchPoliciesQuery request, CancellationToken cancellationToken)
    {
        var results = await policies.SearchAsync(request.Query, cancellationToken);
        return mapper.Map<List<PolicyDto>>(results);
    }
}

/// <summary>Returns the coverage types for a policy (FRS §10 GET /api/policies/{id}/coverage).</summary>
public sealed record GetPolicyCoverageQuery(Guid PolicyId) : IRequest<IReadOnlyList<string>>;

public sealed class GetPolicyCoverageQueryHandler(IPolicyRepository policies)
    : IRequestHandler<GetPolicyCoverageQuery, IReadOnlyList<string>>
{
    public async Task<IReadOnlyList<string>> Handle(GetPolicyCoverageQuery request, CancellationToken cancellationToken)
    {
        var policy = await policies.GetByIdAsync(request.PolicyId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.Policy), request.PolicyId);

        return policy.CoverageTypes
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
