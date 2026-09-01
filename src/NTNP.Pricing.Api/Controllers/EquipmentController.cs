using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTNP.Pricing.Api.Authorization;
using NTNP.Pricing.Application.Equipment;
using NTNP.Pricing.Contracts.Common;
using NTNP.Pricing.Contracts.Equipment;

namespace NTNP.Pricing.Api.Controllers;

/// <summary>Section 9 — the central Equipment Database (replaces "SOURCE PRICE DEVICES").</summary>
[ApiController]
[Route("api/equipment")]
public sealed class EquipmentController : ControllerBase
{
    private readonly IEquipmentService _service;
    private readonly IEquipmentImportService _importService;

    public EquipmentController(IEquipmentService service, IEquipmentImportService importService)
    {
        _service = service;
        _importService = importService;
    }

    [HttpGet]
    [Authorize(Policy = PolicyNames.ViewOnly)]
    public async Task<ActionResult<PagedResult<EquipmentDto>>> Search(
        [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 25,
        [FromQuery] bool includeInactive = false, [FromQuery] string? category = null,
        [FromQuery] bool missingPriceOnly = false, CancellationToken ct = default) =>
        Ok(await _service.SearchAsync(new PagedQuery(search, page, pageSize), includeInactive, category, missingPriceOnly, ct));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = PolicyNames.ViewOnly)]
    public async Task<ActionResult<EquipmentDto>> Get(Guid id, CancellationToken ct) => Ok(await _service.GetAsync(id, ct));

    [HttpGet("reports/missing-or-expired-prices")]
    [Authorize(Policy = PolicyNames.ViewOnly)]
    public async Task<ActionResult<IReadOnlyList<EquipmentDto>>> MissingOrExpiredPriceReport([FromQuery] int staleDays = 180, CancellationToken ct = default) =>
        Ok(await _service.GetMissingOrExpiredPriceReportAsync(staleDays, ct));

    [HttpPost]
    [Authorize(Policy = PolicyNames.ManageEquipment)]
    public async Task<ActionResult<EquipmentDto>> Create(CreateEquipmentRequest request, CancellationToken ct)
    {
        var result = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = PolicyNames.ManageEquipment)]
    public async Task<ActionResult<EquipmentDto>> Update(Guid id, UpdateEquipmentRequest request, CancellationToken ct) =>
        Ok(await _service.UpdateAsync(id, request, ct));

    [HttpPost("bulk-activate")]
    [Authorize(Policy = PolicyNames.ManageEquipment)]
    public async Task<IActionResult> BulkSetActive([FromBody] BulkActivateRequest request, CancellationToken ct)
    {
        await _service.BulkSetActiveAsync(request.Ids, request.IsActive, ct);
        return NoContent();
    }

    [HttpGet("{id:guid}/prices")]
    [Authorize(Policy = PolicyNames.ViewOnly)]
    public async Task<ActionResult<IReadOnlyList<EquipmentPriceDto>>> PriceHistory(Guid id, CancellationToken ct) =>
        Ok(await _service.GetPriceHistoryAsync(id, ct));

    [HttpPost("prices")]
    [Authorize(Policy = PolicyNames.ManageEquipment)]
    public async Task<ActionResult<EquipmentPriceDto>> AddPrice(CreateEquipmentPriceRequest request, CancellationToken ct) =>
        Ok(await _service.AddPriceAsync(request, ct));

    // --- Excel import (Section 9's 10-step workflow) ---

    [HttpPost("import/preview")]
    [Authorize(Policy = PolicyNames.ManageEquipment)]
    [RequestSizeLimit(50_000_000)]
    public async Task<ActionResult<EquipmentImportPreviewResult>> PreviewImport(IFormFile file, CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();
        return Ok(await _importService.PreviewAsync(stream, file.FileName, ct));
    }

    [HttpPost("import/commit")]
    [Authorize(Policy = PolicyNames.ManageEquipment)]
    public async Task<ActionResult<EquipmentImportCommitResult>> CommitImport(EquipmentImportCommitRequest request, CancellationToken ct) =>
        Ok(await _importService.CommitAsync(request, ct));
}

public sealed record BulkActivateRequest(IReadOnlyList<Guid> Ids, bool IsActive);
