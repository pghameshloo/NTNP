namespace NTNP.Pricing.Domain.Exceptions;

/// <summary>
/// Section 6 — raised for a failed login (unknown user/inactive account/locked-out/bad password) or
/// an invalid/expired/already-rotated refresh token. Deliberately distinct from
/// <see cref="DomainValidationException"/> (which the Api layer maps to 400/422, implying a
/// malformed request) — the Api layer maps this one to 401 Unauthorized, matching how the JWT
/// bearer middleware itself reports "not authenticated" for a missing/invalid access token, so
/// authentication failures have one consistent status code across the whole auth surface.
/// </summary>
public sealed class AuthenticationFailedException : Exception
{
    public AuthenticationFailedException(string message) : base(message)
    {
    }
}
