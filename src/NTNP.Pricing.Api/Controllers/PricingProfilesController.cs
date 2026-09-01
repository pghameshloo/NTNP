using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTNP.Pricing.Api.Authorization;
using NTNP.Pricing.Application.PricingProfiles;
using NTNP.Pricing.Contracts.PricingProfiles;

namespace NTNP.Pricing.Api.Controllers;

/// <summary>Section 12 — Pricing Profiles and Settings.</summary>
[ApiController]
[Route("api/pricing-profiles")]
public sealed class PricingProfilesController : ControllerBase
{
    private readonly IPricingProfileService _service;

    public PricingProfilesController(IPricingProfileService service) => _service = service;

    [HttpGet]
    [Authorize(Policy = PolicyNames.ViewOnly)]
    public async Task<ActionResult<IReadOnlyList<PricingProfileDto>>> List([FromQuery] bool includeInactive = false, CancellationToken ct = default) =>
        Ok(await _service.ListAsync(includeInactive, ct));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = PolicyNames.ViewOnly)]
    public async Task<ActionResult<PricingProfileDto>> Get(Guid id, CancellationToken ct) => Ok(await _service.GetAsync(id, ct));

    [HttpPost]
    [Authorize(Policy = PolicyNames.ManagePricingProfiles)]
    public async Task<ActionResult<PricingProfileDto>> Create(UpsertPricingProfileRequest request, CancellationToken ct)
    {
        var result = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = PolicyNames.ManagePricingProfiles)]
    public async Task<ActionResult<PricingProfileDto>> Update(Guid id, UpsertPricingProfileRequest request, CancellationToken ct) =>
        Ok(await _service.UpdateAsync(id, request, ct));
}
