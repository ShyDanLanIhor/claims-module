namespace ClaimsModule.Application.Common.Exceptions;

/// <summary>
/// Raised when one or more FluentValidation rules fail. Maps to HTTP 422 with the structured
/// error body defined in FRS §10.4.
/// </summary>
public sealed class ValidationException : Exception
{
    public ValidationException()
        : base("One or more validation errors occurred.")
        => Errors = new Dictionary<string, string[]>();

    public ValidationException(IReadOnlyDictionary<string, string[]> errors)
        : this() => Errors = errors;

    public IReadOnlyDictionary<string, string[]> Errors { get; }
}

/// <summary>
/// Raised when a business rule that needs cross-entity context fails (e.g. closure conditions,
/// self-approval, authority). Maps to HTTP 422 and carries field-keyed messages.
/// </summary>
public sealed class BusinessRuleException : Exception
{
    public BusinessRuleException(string message) : base(message)
        => Errors = new Dictionary<string, string[]> { ["_"] = [message] };

    public BusinessRuleException(string key, string message) : base(message)
        => Errors = new Dictionary<string, string[]> { [key] = [message] };

    public BusinessRuleException(IReadOnlyDictionary<string, string[]> errors)
        : base("One or more business rules were not satisfied.") => Errors = errors;

    public IReadOnlyDictionary<string, string[]> Errors { get; }
}

/// <summary>Raised when an entity referenced by id does not exist. Maps to HTTP 404.</summary>
public sealed class NotFoundException(string name, object key)
    : Exception($"\"{name}\" ({key}) was not found.");

/// <summary>Raised when the current user lacks the role/authority for an action. Maps to HTTP 403.</summary>
public sealed class ForbiddenAccessException(string message) : Exception(message);
