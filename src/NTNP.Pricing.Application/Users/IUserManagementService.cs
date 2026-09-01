using NTNP.Pricing.Contracts.Auth;

namespace NTNP.Pricing.Application.Users;

/// <summary>
/// Section 6/22 — Users and Roles administration. Implemented in Infrastructure because it wraps
/// ASP.NET Core Identity's <c>UserManager</c>/<c>RoleManager</c>, which Application does not
/// reference directly.
/// </summary>
public interface IUserManagementService
{
    Task<IReadOnlyList<UserDto>> ListUsersAsync(CancellationToken ct = default);
    Task<UserDto> GetUserAsync(Guid id, CancellationToken ct = default);
    Task<UserDto> CreateUserAsync(CreateUserRequest request, CancellationToken ct = default);
    Task<UserDto> UpdateUserAsync(Guid id, UpdateUserRequest request, CancellationToken ct = default);
    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<RoleDto>> ListRolesAsync(CancellationToken ct = default);
}
