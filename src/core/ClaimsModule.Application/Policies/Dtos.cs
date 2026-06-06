using ClaimsModule.Domain.Enums;

namespace ClaimsModule.Application.Policies;

/// <summary>A simulated policy returned from lookup (FRS §10.3 GET /api/policies/search).</summary>
public sealed record PolicyDto
{
    public Guid Id { get; init; }
    public string PolicyNumber { get; init; } = string.Empty;
    public string ClientName { get; init; } = string.Empty;
    public DateOnly EffectiveDate { get; init; }
    public DateOnly ExpirationDate { get; init; }
    public PolicyStatus Status { get; init; }
    public IReadOnlyList<string> CoverageTypes { get; init; } = [];
}
