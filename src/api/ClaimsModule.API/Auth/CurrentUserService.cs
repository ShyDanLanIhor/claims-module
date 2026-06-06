using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Domain.Common;
using ClaimsModule.Domain.Enums;

namespace ClaimsModule.API.Auth;

/// <summary>
/// Resolves the current user from the request. The assessment uses a mock auth scheme (FRS §3): the
/// Angular HTTP interceptor sends identity via headers (X-User-Id / X-User-Name / X-User-Role) and a
/// Bearer token; here we read those headers, defaulting to a Handler. The tenant is the single seeded
/// organisation. Correlation id is read from the request (or generated once) and cached per request
/// so all audit entries in a request share it (FRS §14.2).
/// </summary>
public sealed class CurrentUserService(IHttpContextAccessor accessor) : ICurrentUserService
{
    private HttpContext? Http => accessor.HttpContext;

    // No HttpContext => a background (Hangfire) scope: the actor is the system (Guid.Empty -> null
    // actor in the audit log). An interactive request with no header defaults to the mock Handler.
    public Guid UserId =>
        Http is null ? Guid.Empty
        : TryGuid(Header("X-User-Id"), out var id) ? id : SeedIdentifiers.HandlerUserId;

    public string? UserName => Header("X-User-Name") ?? "dev.handler";

    public UserRole Role =>
        Enum.TryParse<UserRole>(Header("X-User-Role"), ignoreCase: true, out var role) ? role : UserRole.Handler;

    public Guid OrganisationId => SeedIdentifiers.OrganisationId;

    public Guid CorrelationId
    {
        get
        {
            if (Http is null)
                return Guid.Empty;

            const string key = "CorrelationId";
            if (Http.Items.TryGetValue(key, out var existing) && existing is Guid cached)
                return cached;

            var correlation = TryGuid(Header("X-Correlation-Id"), out var fromHeader) ? fromHeader : Guid.NewGuid();
            Http.Items[key] = correlation;
            return correlation;
        }
    }

    public bool IsInRole(UserRole role) => (int)Role >= (int)role;

    private string? Header(string name)
    {
        var value = Http?.Request.Headers[name].ToString();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static bool TryGuid(string? value, out Guid result)
    {
        result = Guid.Empty;
        return value is not null && Guid.TryParse(value, out result);
    }
}
