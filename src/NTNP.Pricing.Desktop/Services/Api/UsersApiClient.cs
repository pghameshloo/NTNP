using NTNP.Pricing.Contracts.Auth;

namespace NTNP.Pricing.Desktop.Services.Api;

public sealed class UsersApiClient : ApiClientBase
{
    public UsersApiClient(HttpClient http, IServerConnectionSettingsService serverSettings, AppSession session) : base(http, serverSettings, session)
    {
    }

    public Task<IReadOnlyList<UserDto>> ListAsync(CancellationToken ct = default) => GetAsync<IReadOnlyList<UserDto>>("api/users", ct);
    public Task<UserDto> GetAsync(Guid id, CancellationToken ct = default) => GetAsync<UserDto>($"api/users/{id}", ct);
    public Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken ct = default) => PostAsync<UserDto>("api/users", request, ct);
    public Task<UserDto> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken ct = default) => PutAsync<UserDto>($"api/users/{id}", request, ct);
    public Task ResetPasswordAsync(Guid id, string newPassword, CancellationToken ct = default) => PostAsync($"api/users/{id}/reset-password", newPassword, ct);
    public Task<IReadOnlyList<RoleDto>> ListRolesAsync(CancellationToken ct = default) => GetAsync<IReadOnlyList<RoleDto>>("api/roles", ct);
}
