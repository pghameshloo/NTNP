using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;

namespace NTNP.Pricing.Api.Middleware;

/// <summary>
/// Runs any registered <see cref="IValidator{T}"/> against every matching action argument before
/// the action executes (Section 1: FluentValidation is required server-side). A failure throws
/// <see cref="ValidationException"/>, translated to a 400 by <see cref="ExceptionHandlingMiddleware"/>.
/// Requests with no registered validator for their DTO type pass through untouched — Application
/// services still enforce their own business-rule (DB-backed) validation regardless.
/// </summary>
public sealed class ValidationActionFilter : IAsyncActionFilter
{
    private readonly IServiceProvider _serviceProvider;

    public ValidationActionFilter(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null) continue;

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (_serviceProvider.GetService(validatorType) is not IValidator validator) continue;

            var validationContext = new ValidationContext<object>(argument);
            var result = await validator.ValidateAsync(validationContext, context.HttpContext.RequestAborted);
            if (!result.IsValid) throw new ValidationException(result.Errors);
        }

        await next();
    }
}
