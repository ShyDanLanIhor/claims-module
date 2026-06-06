namespace ClaimsModule.Application.Common.Interfaces;

/// <summary>A previously-recorded response for an idempotent write (FRS §10 Idempotency-Key).</summary>
public sealed record IdempotentResponse(int StatusCode, string? Body, string? ContentType);

/// <summary>
/// Records and replays responses for write requests that carry an <c>Idempotency-Key</c> header, so a
/// retried request returns the original response instead of re-executing the operation (FRS §10).
/// </summary>
public interface IIdempotencyStore
{
    /// <summary>Returns the recorded response for <paramref name="key"/> in the tenant, or null if unseen.</summary>
    Task<IdempotentResponse?> TryGetAsync(Guid organisationId, string key, CancellationToken cancellationToken = default);

    /// <summary>Records the response for a key. Swallows a concurrent duplicate (unique key) race.</summary>
    Task SaveAsync(Guid organisationId, string key, string method, string path,
        int statusCode, string? body, string? contentType, CancellationToken cancellationToken = default);
}
