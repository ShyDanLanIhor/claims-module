using FluentValidation;
using MediatR;
using ValidationException = ClaimsModule.Application.Common.Exceptions.ValidationException;

namespace ClaimsModule.Application.Common.Behaviours;

/// <summary>
/// MediatR pipeline behaviour that runs all FluentValidation validators for a request before its
/// handler executes (FRS expectation: validation wired at the MediatR pipeline level, not the
/// controller). Aggregated failures are thrown as a structured <see cref="ValidationException"/>.
/// </summary>
public sealed class ValidationBehaviour<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = results
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count != 0)
        {
            var errors = failures
                .GroupBy(f => f.PropertyName, f => f.ErrorMessage)
                .ToDictionary(g => g.Key, g => g.Distinct().ToArray());

            throw new ValidationException(errors);
        }

        return await next();
    }
}
