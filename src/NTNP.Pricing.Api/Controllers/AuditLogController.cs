using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTNP.Pricing.Api.Authorization;
using NTNP.Pricing.Application.Audit;
using NTNP.Pricing.Contracts.Audit;
using NTNP.Pricing.Contracts.Common;

namespace NTNP.Pricing.Api.Controllers;

/// <summary>Section 30 — Audit Logs screen (Admin only).</summary>
[ApiController]
[Route("api/audit-log")]
[Authorize(Policy = PolicyNames.ViewAuditLog)]
public sealed class AuditLogController : ControllerBase
{
    private readonly IAuditQueryService _service;

    public AuditLogController(IAuditQueryService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<PagedResult<AuditLogEntryDto>>> Search([FromQuery] AuditLogQuery query, CancellationToken ct) =>
        Ok(await _service.SearchAsync(query, ct));
}
