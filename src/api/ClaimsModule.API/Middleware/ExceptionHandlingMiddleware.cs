using System.Text.Json;
using ClaimsModule.Application.Common.Exceptions;
using ClaimsModule.Domain.Common;
using Microsoft.EntityFrameworkCore;
using ValidationException = ClaimsModule.Application.Common.Exceptions.ValidationException;

namespace ClaimsModule.API.Middleware;

/// <summary>
/// Translates exceptions into the consistent structured error body from FRS §10.4. Validation and
/// business-rule failures become 422; not-found 404; forbidden 403; concurrency 409; everything else 500.
/// </summary>
public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await WriteAsync(context, ex);
        }
    }

    private async Task WriteAsync(HttpContext context, Exception exception)
    {
        // If the response has already begun streaming we cannot rewrite headers/body — attempting to
        // would throw a second exception and corrupt the response. Log and bail (standard pattern).
        if (context.Response.HasStarted)
        {
            logger.LogError(exception, "Response already started; cannot write error body for {Method} {Path}",
                context.Request.Method, context.Request.Path);
            return;
        }

        var (status, type, title, errors) = Map(exception);

        if (status >= StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled exception processing {Method} {Path}", context.Request.Method, context.Request.Path);
        else
            logger.LogWarning(exception, "Request {Method} {Path} mapped to {Status} ({ExceptionType})",
                context.Request.Method, context.Request.Path, status, exception.GetType().Name);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = status;

        var payload = new
        {
            type,
            title,
            status,
            errors,
            traceId = context.TraceIdentifier
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions));
    }

    private static (int Status, string Type, string Title, IReadOnlyDictionary<string, string[]>? Errors) Map(Exception exception) =>
        exception switch
        {
            ValidationException v => (StatusCodes.Status422UnprocessableEntity, "ValidationError",
                "One or more validation errors occurred.", v.Errors),

            BusinessRuleException b => (StatusCodes.Status422UnprocessableEntity, "BusinessRuleError",
                "One or more business rules were not satisfied.", b.Errors),

            DomainException d => (StatusCodes.Status422UnprocessableEntity, "DomainError",
                d.Message, new Dictionary<string, string[]> { ["_"] = [d.Message] }),

            NotFoundException n => (StatusCodes.Status404NotFound, "NotFound", n.Message, null),

            ForbiddenAccessException f => (StatusCodes.Status403Forbidden, "Forbidden", f.Message, null),

            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "ConcurrencyConflict",
                "The record was modified by another user. Reload and try again.", null),

            _ => (StatusCodes.Status500InternalServerError, "ServerError", "An unexpected error occurred.", null)
        };
}
