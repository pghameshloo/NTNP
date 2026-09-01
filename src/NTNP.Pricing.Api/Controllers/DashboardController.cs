using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTNP.Pricing.Api.Authorization;
using NTNP.Pricing.Application.Dashboard;
using NTNP.Pricing.Contracts.Dashboard;

namespace NTNP.Pricing.Api.Controllers;

/// <summary>Section 24 — dashboard KPIs and charts.</summary>
[ApiController]
[Route("api/dashboard")]
[Authorize(Policy = PolicyNames.ViewOnly)]
public sealed class DashboardController : ControllerBase
{
    private readonly IDashboardService _service;

    public DashboardController(IDashboardService service) => _service = service;

    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryDto>> Summary(CancellationToken ct) => Ok(await _service.GetSummaryAsync(ct));
}
