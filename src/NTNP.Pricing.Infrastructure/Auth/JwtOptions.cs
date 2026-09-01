namespace NTNP.Pricing.Infrastructure.Auth;

/// <summary>Section 6 — JWT access tokens + rotating refresh tokens. Bound from configuration section "Jwt".</summary>
public sealed class JwtOptions
{
    public string SigningKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = "NTNP.Pricing.Api";
    public string Audience { get; set; } = "NTNP.Pricing.Clients";
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 7;
}
