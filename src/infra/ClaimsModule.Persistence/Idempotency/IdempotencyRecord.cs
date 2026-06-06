using ClaimsModule.Domain.Common;

namespace ClaimsModule.Persistence.Idempotency;

/// <summary>
/// Persisted response for an idempotent write request (FRS §10). An infrastructure record, not a
/// domain entity — keyed by (OrganisationId, Key) so a retried request replays the original response.
/// </summary>
public sealed class IdempotencyRecord
{
    public Guid Id { get; set; } = SequentialGuid.Create();
    public Guid OrganisationId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public string? ResponseBody { get; set; }
    public string? ContentType { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
