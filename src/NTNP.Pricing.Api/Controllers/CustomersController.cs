using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTNP.Pricing.Application.Customers;
using NTNP.Pricing.Api.Authorization;
using NTNP.Pricing.Contracts.Common;
using NTNP.Pricing.Contracts.Customers;

namespace NTNP.Pricing.Api.Controllers;

/// <summary>Section 7 — Customer database.</summary>
[ApiController]
[Route("api/customers")]
public sealed class CustomersController : ControllerBase
{
    private readonly ICustomerService _service;

    public CustomersController(ICustomerService service) => _service = service;

    [HttpGet]
    [Authorize(Policy = PolicyNames.ViewOnly)]
    public async Task<ActionResult<PagedResult<CustomerDto>>> Search(
        [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 25,
        [FromQuery] bool includeInactive = false, CancellationToken ct = default) =>
        Ok(await _service.SearchAsync(new PagedQuery(search, page, pageSize), includeInactive, ct));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = PolicyNames.ViewOnly)]
    public async Task<ActionResult<CustomerDto>> Get(Guid id, CancellationToken ct) => Ok(await _service.GetAsync(id, ct));

    [HttpGet("duplicates")]
    [Authorize(Policy = PolicyNames.ManageCustomers)]
    public async Task<ActionResult<IReadOnlyList<CustomerDuplicateCandidate>>> FindDuplicates(
        [FromQuery] string companyName, [FromQuery] string? email, [FromQuery] string? phone, CancellationToken ct) =>
        Ok(await _service.FindDuplicatesAsync(companyName, email, phone, ct));

    [HttpPost]
    [Authorize(Policy = PolicyNames.ManageCustomers)]
    public async Task<ActionResult<CustomerDto>> Create(CreateCustomerRequest request, CancellationToken ct)
    {
        var result = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = PolicyNames.ManageCustomers)]
    public async Task<ActionResult<CustomerDto>> Update(Guid id, UpdateCustomerRequest request, CancellationToken ct) =>
        Ok(await _service.UpdateAsync(id, request, ct));

    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Policy = PolicyNames.ManageCustomers)]
    public async Task<IActionResult> Deactivate(Guid id, [FromBody] byte[] rowVersion, CancellationToken ct)
    {
        await _service.DeactivateAsync(id, rowVersion, ct);
        return NoContent();
    }
}
