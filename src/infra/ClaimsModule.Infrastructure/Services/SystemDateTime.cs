using ClaimsModule.Application.Common.Interfaces;

namespace ClaimsModule.Infrastructure.Services;

/// <summary>Real system clock (UTC). Abstracted via <see cref="IDateTime"/> for deterministic tests.</summary>
public sealed class SystemDateTime : IDateTime
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
