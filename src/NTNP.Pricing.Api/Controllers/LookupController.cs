using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTNP.Pricing.Api.Authorization;
using NTNP.Pricing.Application.PanelTemplates;
using NTNP.Pricing.Contracts.PanelTemplates;

namespace NTNP.Pricing.Api.Controllers;

/// <summary>Section 3 — admin-editable Product Families and Panel Types (never hardcoded).</summary>
[ApiController]
[Route("api/lookups")]
public sealed class LookupController : ControllerBase
{
    private readonly ILookupService _service;

    public LookupController(ILookupService service) => _service = service;

    [HttpGet("product-families")]
    [Authorize(Policy = PolicyNames.ViewOnly)]
    public async Task<ActionResult<IReadOnlyList<ProductFamilyDto>>> ProductFamilies([FromQuery] bool includeInactive = false, CancellationToken ct = default) =>
        Ok(await _service.ListProductFamiliesAsync(includeInactive, ct));

    [HttpPost("product-families")]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    public async Task<ActionResult<ProductFamilyDto>> CreateProductFamily(CreateProductFamilyRequest request, CancellationToken ct) =>
        Ok(await _service.CreateProductFamilyAsync(request.Code, request.Name, request.VoltageRangeDescription, request.SwitchgearClass, ct));

    [HttpGet("panel-types")]
    [Authorize(Policy = PolicyNames.ViewOnly)]
    public async Task<ActionResult<IReadOnlyList<PanelTypeDto>>> PanelTypes([FromQuery] bool includeInactive = false, CancellationToken ct = default) =>
        Ok(await _service.ListPanelTypesAsync(includeInactive, ct));

    [HttpPost("panel-types")]
    [Authorize(Policy = PolicyNames.AdminOnly)]
    public async Task<ActionResult<PanelTypeDto>> CreatePanelType(CreatePanelTypeRequest request, CancellationToken ct) =>
        Ok(await _service.CreatePanelTypeAsync(request.Code, request.Name, request.Description, request.SortOrder, ct));
}

public sealed record CreateProductFamilyRequest(string Code, string Name, string? VoltageRangeDescription, string? SwitchgearClass);
public sealed record CreatePanelTypeRequest(string Code, string Name, string? Description, int SortOrder);
