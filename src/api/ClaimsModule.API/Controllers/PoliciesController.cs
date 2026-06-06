using ClaimsModule.Application.Policies;
using Microsoft.AspNetCore.Mvc;

namespace ClaimsModule.API.Controllers;

[Route("api/policies")]
public sealed class PoliciesController : ApiControllerBase
{
    /// <summary>Search the simulated policy dataset by number or client name (FRS §10.3).</summary>
    [HttpGet("search")]
    public Task<IReadOnlyList<PolicyDto>> Search([FromQuery] string? q, CancellationToken ct) =>
        Mediator.Send(new SearchPoliciesQuery(q), ct);

    /// <summary>Coverage types available on a policy (used during FNOL).</summary>
    [HttpGet("{id:guid}/coverage")]
    public Task<IReadOnlyList<string>> GetCoverage(Guid id, CancellationToken ct) =>
        Mediator.Send(new GetPolicyCoverageQuery(id), ct);
}
