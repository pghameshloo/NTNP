using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTNP.Pricing.Application.Auth;
using NTNP.Pricing.Contracts.Auth;

namespace NTNP.Pricing.Api.Controllers;

/// <summary>Section 6 — login and refresh-token rotation. The desktop client's Login screen calls these.</summary>
[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService) => _authService = authService;

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken ct) =>
        Ok(await _authService.LoginAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString(), ct));

    [HttpPost("refresh")]
    public async Task<ActionResult<LoginResponse>> Refresh(RefreshTokenRequest request, CancellationToken ct) =>
        Ok(await _authService.RefreshAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString(), ct));

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RefreshTokenRequest request, CancellationToken ct)
    {
        await _authService.RevokeRefreshTokenAsync(request.RefreshToken, HttpContext.Connection.RemoteIpAddress?.ToString(), ct);
        return NoContent();
    }
}
