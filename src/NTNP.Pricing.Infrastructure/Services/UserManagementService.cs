using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NTNP.Pricing.Application.Common;
using NTNP.Pricing.Application.Exceptions;
using NTNP.Pricing.Application.Users;
using NTNP.Pricing.Contracts.Auth;
using NTNP.Pricing.Domain.Enums;
using NTNP.Pricing.Domain.Exceptions;
using NTNP.Pricing.Infrastructure.Identity;

namespace NTNP.Pricing.Infrastructure.Services;

/// <summary>Section 6/22 — Users and Roles administration screen.</summary>
public sealed class UserManagementService : IUserManagementService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IAuditService _audit;
    private readonly ICurrentUserService _currentUser;

    public UserManagementService(
        UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager, IAuditService audit, ICurrentUserService currentUser)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _audit = audit;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<UserDto>> ListUsersAsync(CancellationToken ct = default)
    {
        var users = await _userManager.Users.OrderBy(u => u.DisplayName).ToListAsync(ct);
        var result = new List<UserDto>(users.Count);
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            result.Add(ToDto(user, roles));
        }
        return result;
    }

    public async Task<UserDto> GetUserAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(id.ToString()) ?? throw new NotFoundException("User", id);
        var roles = await _userManager.GetRolesAsync(user);
        return ToDto(user, roles);
    }

    public async Task<UserDto> CreateUserAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        foreach (var role in request.Roles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
                throw new DomainValidationException($"Unknown role '{role}'.");
        }

        var user = new ApplicationUser
        {
            UserName = request.UserName,
            Email = request.Email,
            DisplayName = request.DisplayName,
            EmailConfirmed = true,
            IsActive = true,
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
            throw new DomainValidationException(createResult.Errors.Select(e => e.Description).ToList());

        if (request.Roles.Count > 0)
            await _userManager.AddToRolesAsync(user, request.Roles);

        await _audit.LogAsync(AuditAction.Created, "User", user.Id.ToString(), newValue: new { user.UserName, request.Roles }, cancellationToken: ct);

        return ToDto(user, request.Roles);
    }

    public async Task<UserDto> UpdateUserAsync(Guid id, UpdateUserRequest request, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(id.ToString()) ?? throw new NotFoundException("User", id);

        user.DisplayName = request.DisplayName;
        user.IsActive = request.IsActive;
        await _userManager.UpdateAsync(user);

        var currentRoles = await _userManager.GetRolesAsync(user);
        var toRemove = currentRoles.Except(request.Roles).ToList();
        var toAdd = request.Roles.Except(currentRoles).ToList();
        if (toRemove.Count > 0) await _userManager.RemoveFromRolesAsync(user, toRemove);
        if (toAdd.Count > 0) await _userManager.AddToRolesAsync(user, toAdd);

        await _audit.LogAsync(AuditAction.UserRoleChanged, "User", user.Id.ToString(),
            oldValue: new { Roles = currentRoles }, newValue: new { Roles = request.Roles }, cancellationToken: ct);

        return ToDto(user, request.Roles);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString()) ?? throw new NotFoundException("User", request.UserId);

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, request.NewPassword);
        if (!result.Succeeded)
            throw new DomainValidationException(result.Errors.Select(e => e.Description).ToList());

        // Never log the new password itself — only that a reset occurred (Section 30).
        await _audit.LogAsync(AuditAction.Updated, "User", user.Id.ToString(), reason: "Password reset by administrator", cancellationToken: ct);
    }

    public async Task<IReadOnlyList<RoleDto>> ListRolesAsync(CancellationToken ct = default)
    {
        var roles = await _roleManager.Roles.ToListAsync(ct);
        return roles.Select(r => new RoleDto(r.Name ?? string.Empty, r.Description)).ToList();
    }

    private static UserDto ToDto(ApplicationUser user, IEnumerable<string> roles) => new(
        user.Id, user.UserName ?? string.Empty, user.Email ?? string.Empty, user.DisplayName,
        roles.ToList(), user.IsActive, user.LastLoginAtUtc);
}
