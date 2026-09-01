using NTNP.Pricing.Contracts.Auth;

namespace NTNP.Pricing.Desktop.Services.Api;

public sealed class AuthApiClient : ApiClientBase
{
    public AuthApiClient(HttpClient http, IServerConnectionSettingsService serverSettings, AppSession session) : base(http, serverSettings, session)
    {
    }

    public Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default) => PostAsync<LoginResponse>("api/auth/login", request, ct);
    public Task<LoginResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken ct = default) => PostAsync<LoginResponse>("api/auth/refresh", request, ct);
    public Task LogoutAsync(RefreshTokenRequest request, CancellationToken ct = default) => PostAsync("api/auth/logout", request, ct);
}
