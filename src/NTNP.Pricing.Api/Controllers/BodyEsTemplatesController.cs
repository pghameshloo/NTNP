using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTNP.Pricing.Api.Authorization;
using NTNP.Pricing.Application.BodyEs;
using NTNP.Pricing.Contracts.BodyEs;
using NTNP.Pricing.Contracts.Common;

namespace NTNP.Pricing.Api.Controllers;

/// <summary>Section 11 — BODY+ES module.</summary>
[ApiController]
[Route("api/body-es-templates")]
public sealed class BodyEsTemplatesController : ControllerBase
{
    private readonly IBodyEsTemplateService _service;

    public BodyEsTemplatesController(IBodyEsTemplateService service) => _service = service;

    [HttpGet]
    [Authorize(Policy = PolicyNames.ViewOnly)]
    public async Task<ActionResult<PagedResult<BodyEsTemplateDto>>> Search(
        [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 25,
        [FromQuery] Guid? productFamilyId = null, [FromQuery] Guid? panelTypeId = null, CancellationToken ct = default) =>
        Ok(await _service.SearchAsync(new PagedQuery(search, page, pageSize), productFamilyId, panelTypeId, ct));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = PolicyNames.ViewOnly)]
    public async Task<ActionResult<BodyEsTemplateDto>> Get(Guid id, CancellationToken ct) => Ok(await _service.GetAsync(id, ct));

    [HttpPost]
    [Authorize(Policy = PolicyNames.ManageTemplates)]
    public async Task<ActionResult<BodyEsTemplateDto>> Create(CreateBodyEsTemplateRequest request, CancellationToken ct)
    {
        var result = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = PolicyNames.ManageTemplates)]
    public async Task<ActionResult<BodyEsTemplateDto>> Update(Guid id, UpdateBodyEsTemplateRequest request, CancellationToken ct) =>
        Ok(await _service.UpdateAsync(id, request, ct));

    [HttpPost("{id:guid}/approve")]
    [Authorize(Policy = PolicyNames.ManageTemplates)]
    public async Task<ActionResult<BodyEsTemplateDto>> Approve(Guid id, [FromBody] byte[] rowVersion, CancellationToken ct) =>
        Ok(await _service.ApproveAsync(id, rowVersion, ct));
}
