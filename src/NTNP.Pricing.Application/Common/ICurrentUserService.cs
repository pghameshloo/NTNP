namespace NTNP.Pricing.Application.Common;

/// <summary>
/// Abstracts "who is making this request" away from ASP.NET Core's <c>HttpContext</c>/claims so
/// Application-layer services stay framework-agnostic and unit-testable. Implemented in the Api
/// project from the authenticated JWT's claims.
/// </summary>
public interface ICurrentUserService
{
    Guid UserId { get; }
    string UserName { get; }
    IReadOnlyList<string> Roles { get; }
    bool IsInRole(string role);
    string? IpAddress { get; }
}
