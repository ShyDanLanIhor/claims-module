using System.Text.Json;
using ClaimsModule.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace ClaimsModule.API.Filters;

/// <summary>
/// Implements the FRS §10 <c>Idempotency-Key</c> contract: a write request carrying the header returns
/// the original recorded response on replay instead of re-executing. Only successful (2xx) responses are
/// recorded, scoped per tenant; requests without the header behave normally.
/// </summary>
public sealed class IdempotencyFilter(
    IIdempotencyStore store,
    ICurrentUserService currentUser,
    IOptions<JsonOptions> jsonOptions) : IAsyncActionFilter
{
    private const string HeaderName = "Idempotency-Key";
    private static readonly HashSet<string> WriteMethods =
        new(StringComparer.OrdinalIgnoreCase) { "POST", "PUT", "PATCH", "DELETE" };

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var request = context.HttpContext.Request;
        if (!WriteMethods.Contains(request.Method)
            || !request.Headers.TryGetValue(HeaderName, out var header)
            || string.IsNullOrWhiteSpace(header))
        {
            await next();
            return;
        }

        var key = header.ToString();
        var org = currentUser.OrganisationId;
        var ct = context.HttpContext.RequestAborted;

        var existing = await store.TryGetAsync(org, key, ct);
        if (existing is not null)
        {
            context.Result = new ContentResult
            {
                StatusCode = existing.StatusCode,
                Content = existing.Body,
                ContentType = existing.ContentType ?? "application/json"
            };
            return;
        }

        var executed = await next();
        if (executed.Exception is not null || executed.Result is null)
            return;

        var (status, body, contentType) = Describe(executed.Result, jsonOptions.Value.JsonSerializerOptions);
        if (status is >= 200 and < 300)
            await store.SaveAsync(org, key, request.Method, request.Path, status, body, contentType, ct);
    }

    private static (int Status, string? Body, string? ContentType) Describe(
        IActionResult result, JsonSerializerOptions options) =>
        result switch
        {
            ObjectResult o => (o.StatusCode ?? StatusCodes.Status200OK,
                JsonSerializer.Serialize(o.Value, options), "application/json"),
            StatusCodeResult s => (s.StatusCode, null, null),
            _ => (StatusCodes.Status200OK, null, null),
        };
}
