namespace NTNP.Pricing.Domain.Exceptions;

/// <summary>
/// Raised when a domain invariant is violated (e.g. an approved revision would be mutated, a
/// reconciliation check fails, shares do not total 100%). The Application layer translates this
/// into an HTTP 422/400 at the API boundary; it must never be swallowed silently.
/// </summary>
public sealed class DomainValidationException : Exception
{
    public IReadOnlyList<string> Errors { get; }

    public DomainValidationException(string message) : base(message)
    {
        Errors = new[] { message };
    }

    public DomainValidationException(IReadOnlyList<string> errors)
        : base(string.Join("; ", errors))
    {
        Errors = errors;
    }
}
