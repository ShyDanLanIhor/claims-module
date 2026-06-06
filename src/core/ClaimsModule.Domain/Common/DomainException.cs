namespace ClaimsModule.Domain.Common;

/// <summary>
/// Thrown when an invariant guarded by the domain model is violated. Application-level
/// validation (FluentValidation) should catch these conditions first and return a structured
/// 422; a <see cref="DomainException"/> reaching the API indicates a defensive last line of defence.
/// </summary>
public sealed class DomainException(string message) : Exception(message);
