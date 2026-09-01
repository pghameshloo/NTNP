using System.Text.Json;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NTNP.Pricing.Application.Exceptions;
using NTNP.Pricing.Contracts.Common;
using NTNP.Pricing.Domain.Exceptions;
using Serilog;

namespace NTNP.Pricing.Api.Middleware;

/// <summary>
/// Centralized translation of Domain/Application exceptions into HTTP responses, so controllers
/// stay thin. Section 31: a concurrency conflict is surfaced explicitly (never silently swallowed
/// or overwritten) via <see cref="ConcurrencyConflictResponse"/>.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionHandlingMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleAsync(context, ex);
        }
    }

    private static async Task HandleAsync(HttpContext context, Exception exception)
    {
        var traceId = context.TraceIdentifier;
        context.Response.ContentType = "application/json";

        switch (exception)
        {
            case AuthenticationFailedException authFailed:
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await WriteAsync(context, new ApiErrorResponse("authentication-failed", "Authentication failed", 401, new[] { authFailed.Message }, traceId));
                break;

            case NotFoundException notFound:
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                await WriteAsync(context, new ApiErrorResponse("not-found", "Resource not found", 404, new[] { notFound.Message }, traceId));
                break;

            case DomainValidationException domainEx:
                context.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
                await WriteAsync(context, new ApiErrorResponse("validation-error", "Validation failed", 422, domainEx.Errors, traceId));
                break;

            case ValidationException fluentEx:
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                var errors = fluentEx.Errors.Select(e => e.ErrorMessage).ToList();
                await WriteAsync(context, new ApiErrorResponse("validation-error", "Validation failed", 400, errors, traceId));
                break;

            case DbUpdateConcurrencyException:
                context.Response.StatusCode = StatusCodes.Status409Conflict;
                await WriteAsync(context, new ApiErrorResponse("concurrency-conflict", "Concurrency conflict",
                    409, new[] { "This record was changed by another user. Reload it and re-apply your changes (Section 31)." }, traceId));
                break;

            case UnauthorizedAccessException:
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await WriteAsync(context, new ApiErrorResponse("forbidden", "Forbidden", 403, new[] { "You do not have permission to perform this action." }, traceId));
                break;

            default:
                Log.Error(exception, "Unhandled exception. TraceId={TraceId}", traceId);
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await WriteAsync(context, new ApiErrorResponse("server-error", "An unexpected error occurred", 500,
                    new[] { "An unexpected error occurred. Contact your administrator with the trace id." }, traceId));
                break;
        }
    }

    private static Task WriteAsync(HttpContext context, ApiErrorResponse response) =>
        context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
}
