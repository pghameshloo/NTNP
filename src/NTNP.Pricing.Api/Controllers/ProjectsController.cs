using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTNP.Pricing.Api.Authorization;
using NTNP.Pricing.Application.Projects;
using NTNP.Pricing.Contracts.Common;
using NTNP.Pricing.Contracts.Projects;

namespace NTNP.Pricing.Api.Controllers;

/// <summary>Section 13/21 — Project header and the wizard's "Project Information"/"Pricing Settings" steps.</summary>
[ApiController]
[Route("api/projects")]
public sealed class ProjectsController : ControllerBase
{
    private readonly IProjectService _service;

    public ProjectsController(IProjectService service) => _service = service;

    [HttpGet]
    [Authorize(Policy = PolicyNames.ViewOnly)]
    public async Task<ActionResult<PagedResult<ProjectListItemDto>>> Search(
        [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 25,
        [FromQuery] string? status = null, CancellationToken ct = default) =>
        Ok(await _service.SearchAsync(new PagedQuery(search, page, pageSize), status, ct));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = PolicyNames.ViewOnly)]
    public async Task<ActionResult<ProjectDto>> Get(Guid id, CancellationToken ct) => Ok(await _service.GetAsync(id, ct));

    [HttpPost]
    [Authorize(Policy = PolicyNames.ManageProjects)]
    public async Task<ActionResult<ProjectDto>> Create(CreateProjectRequest request, CancellationToken ct)
    {
        var result = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}/info")]
    [Authorize(Policy = PolicyNames.ManageProjects)]
    public async Task<ActionResult<ProjectDto>> UpdateInfo(Guid id, UpdateProjectInfoRequest request, CancellationToken ct) =>
        Ok(await _service.UpdateInfoAsync(id, request, ct));

    [HttpPut("{id:guid}/pricing-settings")]
    [Authorize(Policy = PolicyNames.ManageProjects)]
    public async Task<ActionResult<ProjectRevisionDto>> UpdatePricingSettings(Guid id, UpdateProjectPricingSettingsRequest request, CancellationToken ct) =>
        Ok(await _service.UpdatePricingSettingsAsync(id, request, ct));
}
