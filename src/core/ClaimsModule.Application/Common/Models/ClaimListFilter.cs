using ClaimsModule.Domain.Enums;

namespace ClaimsModule.Application.Common.Models;

/// <summary>Filter/pagination parameters for the claims list (FRS §10.1, GET /api/claims).</summary>
public sealed class ClaimListFilter
{
    public IReadOnlyList<ClaimStatus>? Statuses { get; init; }
    public DateTimeOffset? LossDateFrom { get; init; }
    public DateTimeOffset? LossDateTo { get; init; }
    public Guid? AssignedHandlerId { get; init; }
    public string? CauseOfLossCode { get; init; }
    public Guid? PolicyId { get; init; }

    /// <summary>Partial match on claim number or client name.</summary>
    public string? Search { get; init; }

    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
