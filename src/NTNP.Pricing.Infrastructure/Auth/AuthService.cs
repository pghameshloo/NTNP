using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NTNP.Pricing.Application.Auth;
using NTNP.Pricing.Application.Common;
using NTNP.Pricing.Contracts.Auth;
using NTNP.Pricing.Domain.Enums;
using NTNP.Pricing.Domain.Exceptions;
using NTNP.Pricing.Infrastructure.Identity;
using NTNP.Pricing.Infrastructure.Persistence;

namespace NTNP.Pricing.Infrastructure.Auth;

/// <summary>Section 6 — login, JWT issuance, and secure refresh-token rotation (ASSUMPTIONS.md §7).</summary>
public sealed class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditService _audit;
    private readonly JwtOptions _options;

    public AuthService(
        UserManager<ApplicationUser> userManager, ApplicationDbContext db, IDateTimeProvider clock,
        IAuditService audit, IOptions<JwtOptions> options)
    {
        _userManager = userManager;
        _db = db;
        _clock = clock;
        _audit = audit;
        _options = options.Value;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken ct = default)
    {
        var user = await _userManager.FindByNameAsync(request.UserNameOrEmail) ?? await _userManager.FindByEmailAsync(request.UserNameOrEmail);
        if (user is null || !user.IsActive)
        {
            await _audit.LogAsync(AuditAction.LoginFailed, "User", request.UserNameOrEmail, reason: "User not found or inactive", cancellationToken: ct);
            throw new AuthenticationFailedException("Invalid username or password.");
        }

        if (await _userManager.IsLockedOutAsync(user))
            throw new AuthenticationFailedException("This account is temporarily locked due to repeated failed sign-in attempts.");

        if (!await _userManager.CheckPasswordAsync(user, request.Password))
        {
            await _userManager.AccessFailedAsync(user);
            await _audit.LogAsync(AuditAction.LoginFailed, "User", user.Id.ToString(), cancellationToken: ct);
            throw new AuthenticationFailedException("Invalid username or password.");
        }

        await _userManager.ResetAccessFailedCountAsync(user);
        user.LastLoginAtUtc = _clock.UtcNow;
        await _userManager.UpdateAsync(user);
        await _audit.LogAsync(AuditAction.LoginSucceeded, "User", user.Id.ToString(), cancellationToken: ct);

        return await IssueTokensAsync(user, ipAddress, ct);
    }

    public async Task<LoginResponse> RefreshAsync(RefreshTokenRequest request, string? ipAddress, CancellationToken ct = default)
    {
        var tokenHash = Hash(request.RefreshToken);
        var stored = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);
        if (stored is null || !stored.IsActive)
            throw new AuthenticationFailedException("Refresh token is invalid or expired.");

        var user = await _userManager.FindByIdAsync(stored.UserId.ToString());
        if (user is null || !user.IsActive)
            throw new AuthenticationFailedException("Refresh token is invalid or expired.");

        // Rotate: revoke the presented token, issue a brand-new pair.
        stored.RevokedAtUtc = _clock.UtcNow;
        stored.RevokedByIp = ipAddress;

        var response = await IssueTokensAsync(user, ipAddress, ct);

        var newToken = await _db.RefreshTokens.FirstAsync(t => t.TokenHash == Hash(response.RefreshToken), ct);
        stored.ReplacedByTokenId = newToken.Id;
        await _db.SaveChangesAsync(ct);

        return response;
    }

    public async Task RevokeRefreshTokenAsync(string refreshToken, string? ipAddress, CancellationToken ct = default)
    {
        var tokenHash = Hash(refreshToken);
        var stored = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);
        if (stored is null || !stored.IsActive) return;

        stored.RevokedAtUtc = _clock.UtcNow;
        stored.RevokedByIp = ipAddress;
        await _db.SaveChangesAsync(ct);
    }

    private async Task<LoginResponse> IssueTokensAsync(ApplicationUser user, string? ipAddress, CancellationToken ct)
    {
        var roles = await _userManager.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName ?? user.Email ?? user.Id.ToString()),
            new("displayName", user.DisplayName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAtUtc = _clock.UtcNow.AddMinutes(_options.AccessTokenMinutes);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer, audience: _options.Audience, claims: claims,
            expires: expiresAtUtc.UtcDateTime, signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        var refreshTokenValue = GenerateSecureToken();
        var refreshTokenExpiresAtUtc = _clock.UtcNow.AddDays(_options.RefreshTokenDays);

        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = Hash(refreshTokenValue),
            ExpiresAtUtc = refreshTokenExpiresAtUtc,
            CreatedAtUtc = _clock.UtcNow,
            CreatedByIp = ipAddress,
        });
        await _db.SaveChangesAsync(ct);

        var userDto = new UserDto(user.Id, user.UserName ?? string.Empty, user.Email ?? string.Empty, user.DisplayName,
            roles.ToList(), user.IsActive, user.LastLoginAtUtc);

        return new LoginResponse(accessToken, expiresAtUtc, refreshTokenValue, refreshTokenExpiresAtUtc, userDto);
    }

    private static string GenerateSecureToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    private static string Hash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }
}
