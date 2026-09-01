using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTNP.Pricing.Api.Authorization;
using NTNP.Pricing.Application.Settings;
using NTNP.Pricing.Contracts.Settings;

namespace NTNP.Pricing.Api.Controllers;

/// <summary>Section 23/26/33 — Company and System Settings, Report Template Settings.</summary>
[ApiController]
[Route("api/settings/company")]
public sealed class SettingsController : ControllerBase
{
    private readonly ICompanySettingsService _service;

    public SettingsController(ICompanySettingsService service) => _service = service;

    [HttpGet]
    [Authorize(Policy = PolicyNames.ViewOnly)]
    public async Task<ActionResult<CompanySettingsDto>> Get(CancellationToken ct) => Ok(await _service.GetAsync(ct));

    [HttpPut]
    [Authorize(Policy = PolicyNames.ManageSettings)]
    public async Task<ActionResult<CompanySettingsDto>> Update(UpdateCompanySettingsRequest request, CancellationToken ct) =>
        Ok(await _service.UpdateAsync(request, ct));
}
