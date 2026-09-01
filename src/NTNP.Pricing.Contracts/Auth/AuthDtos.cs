namespace NTNP.Pricing.Contracts.Auth;

public sealed record LoginRequest(string UserNameOrEmail, string Password);

public sealed record LoginResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAtUtc,
    UserDto User);

public sealed record RefreshTokenRequest(string RefreshToken);

public sealed record UserDto(
    Guid Id,
    string UserName,
    string Email,
    string DisplayName,
    IReadOnlyList<string> Roles,
    bool IsActive,
    DateTimeOffset? LastLoginAtUtc);

public sealed record CreateUserRequest(string UserName, string Email, string DisplayName, string Password, IReadOnlyList<string> Roles);

public sealed record UpdateUserRequest(string DisplayName, bool IsActive, IReadOnlyList<string> Roles, byte[] RowVersion);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public sealed record ResetPasswordRequest(Guid UserId, string NewPassword);

public sealed record RoleDto(string Name, string? Description);
