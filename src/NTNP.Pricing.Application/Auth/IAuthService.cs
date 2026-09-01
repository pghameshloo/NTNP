using NTNP.Pricing.Contracts.Auth;

namespace NTNP.Pricing.Application.Auth;

/// <summary>
/// Section 6 — login + JWT access token issuance + refresh-token rotation. Implemented in
/// Infrastructure (wraps ASP.NET Core Identity's <c>UserManager</c> and JWT signing config).
/// </summary>
public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken ct = default);
    Task<LoginResponse> RefreshAsync(RefreshTokenRequest request, string? ipAddress, CancellationToken ct = default);
    Task RevokeRefreshTokenAsync(string refreshToken, string? ipAddress, CancellationToken ct = default);
}
