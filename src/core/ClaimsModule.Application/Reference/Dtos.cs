using ClaimsModule.Domain.Enums;

namespace ClaimsModule.Application.Reference;

/// <summary>An active cause-of-loss code (FRS §10.3 GET /api/reference/cause-of-loss-codes).</summary>
public sealed record CauseOfLossCodeDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public PerilCategory PerilCategory { get; init; }
    public int SortOrder { get; init; }
}

/// <summary>A claim status and its valid next-statuses (FRS §10.3 GET /api/reference/claim-statuses).</summary>
public sealed record ClaimStatusInfoDto
{
    public ClaimStatus Status { get; init; }
    public IReadOnlyList<ClaimStatus> AllowedNextStatuses { get; init; } = [];
}
