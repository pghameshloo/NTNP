using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTNP.Pricing.Api.Authorization;
using NTNP.Pricing.Application.Users;
using NTNP.Pricing.Contracts.Auth;

namespace NTNP.Pricing.Api.Controllers;

/// <summary>Section 6/22 — Users and Roles administration (Admin only).</summary>
[ApiController]
[Route("api/users")]
[Authorize(Policy = PolicyNames.ManageUsers)]
public sealed class UsersController : ControllerBase
{
    private readonly IUserManagementService _service;

    public UsersController(IUserManagementService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> List(CancellationToken ct) => Ok(await _service.ListUsersAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserDto>> Get(Guid id, CancellationToken ct) => Ok(await _service.GetUserAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<UserDto>> Create(CreateUserRequest request, CancellationToken ct)
    {
        var result = await _service.CreateUserAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UserDto>> Update(Guid id, UpdateUserRequest request, CancellationToken ct) =>
        Ok(await _service.UpdateUserAsync(id, request, ct));

    [HttpPost("{id:guid}/reset-password")]
    public async Task<IActionResult> ResetPassword(Guid id, [FromBody] string newPassword, CancellationToken ct)
    {
        await _service.ResetPasswordAsync(new ResetPasswordRequest(id, newPassword), ct);
        return NoContent();
    }

    [HttpGet("~/api/roles")]
    public async Task<ActionResult<IReadOnlyList<RoleDto>>> ListRoles(CancellationToken ct) => Ok(await _service.ListRolesAsync(ct));
}
