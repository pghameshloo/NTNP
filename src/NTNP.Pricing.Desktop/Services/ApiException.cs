using NTNP.Pricing.Contracts.Common;

namespace NTNP.Pricing.Desktop.Services;

/// <summary>
/// Thrown by <see cref="ApiClientBase"/> for any non-success HTTP response. Carries the server's
/// structured <see cref="ApiErrorResponse"/> (Section 6/31 — validation errors, concurrency
/// conflicts, domain rule violations) so view models can show the real reason rather than a generic
/// "something went wrong".
/// </summary>
public sealed class ApiException : Exception
{
    public int StatusCode { get; }
    public ApiErrorResponse? Error { get; }

    public ApiException(int statusCode, ApiErrorResponse? error)
        : base(error is not null && error.Errors.Count > 0 ? string.Join(" ", error.Errors) : error?.Title ?? $"Request failed ({statusCode}).")
    {
        StatusCode = statusCode;
        Error = error;
    }

    public bool IsConcurrencyConflict => StatusCode == 409;
    public bool IsNotFound => StatusCode == 404;
    public bool IsValidation => StatusCode is 400 or 422;
    public bool IsForbidden => StatusCode == 403;
    public bool IsUnauthorized => StatusCode == 401;
}
