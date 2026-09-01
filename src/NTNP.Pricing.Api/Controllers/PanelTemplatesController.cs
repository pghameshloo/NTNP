using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTNP.Pricing.Api.Authorization;
using NTNP.Pricing.Application.PanelTemplates;
using NTNP.Pricing.Contracts.Common;
using NTNP.Pricing.Contracts.PanelTemplates;

namespace NTNP.Pricing.Api.Controllers;

/// <summary>Section 10 — Panel Template and BOM module.</summary>
[ApiController]
[Route("api/panel-templates")]
public sealed class PanelTemplatesController : ControllerBase
{
    private readonly IPanelTemplateService _service;

    public PanelTemplatesController(IPanelTemplateService service) => _service = service;

    [HttpGet]
    [Authorize(Policy = PolicyNames.ViewOnly)]
    public async Task<ActionResult<PagedResult<PanelTemplateDto>>> Search(
        [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 25,
        [FromQuery] Guid? productFamilyId = null, [FromQuery] Guid? panelTypeId = null, CancellationToken ct = default) =>
        Ok(await _service.SearchAsync(new PagedQuery(search, page, pageSize), productFamilyId, panelTypeId, ct));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = PolicyNames.ViewOnly)]
    public async Task<ActionResult<PanelTemplateDto>> Get(Guid id, CancellationToken ct) => Ok(await _service.GetAsync(id, ct));

    [HttpPost]
    [Authorize(Policy = PolicyNames.ManageTemplates)]
    public async Task<ActionResult<PanelTemplateDto>> Create(CreatePanelTemplateRequest request, CancellationToken ct)
    {
        var result = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = PolicyNames.ManageTemplates)]
    public async Task<ActionResult<PanelTemplateDto>> Update(Guid id, UpdatePanelTemplateRequest request, CancellationToken ct) =>
        Ok(await _service.UpdateAsync(id, request, ct));

    [HttpPost("{id:guid}/new-revision")]
    [Authorize(Policy = PolicyNames.ManageTemplates)]
    public async Task<ActionResult<PanelTemplateDto>> CreateNewRevision(Guid id, CancellationToken ct) =>
        Ok(await _service.CreateNewRevisionAsync(id, ct));

    [HttpPost("{id:guid}/approve")]
    [Authorize(Policy = PolicyNames.ManageTemplates)]
    public async Task<ActionResult<PanelTemplateDto>> Approve(Guid id, ApproveTemplateRequest request, CancellationToken ct) =>
        Ok(await _service.ApproveAsync(id, request, ct));
}
