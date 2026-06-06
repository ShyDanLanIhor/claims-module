using ClaimsModule.Domain.Enums;

namespace ClaimsModule.Application.Common.Models;

/// <summary>
/// A non-blocking (Warning) or blocking (Critical) issue surfaced during FNOL intake (FRS §5.4).
/// Critical issues are enforced as FluentValidation errors and never reach a created claim;
/// Warnings are returned to the caller and recorded in the audit log.
/// </summary>
public sealed record ValidationIssue(string Code, string Message, ValidationSeverity Severity)
{
    public static ValidationIssue Warning(string code, string message) =>
        new(code, message, ValidationSeverity.Warning);

    public static ValidationIssue Critical(string code, string message) =>
        new(code, message, ValidationSeverity.Critical);
}
