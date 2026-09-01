using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTNP.Pricing.Api.Authorization;
using NTNP.Pricing.Application.Projects;
using NTNP.Pricing.Contracts.Mto;
using NTNP.Pricing.Contracts.Projects;

namespace NTNP.Pricing.Api.Controllers;

/// <summary>
/// Section 13/16/19/21 — project revisions, the TOTAL screen (via <see cref="ProjectRevisionDto.Totals"/>),
/// the Automatic BOM Generator's line-management operations, and the Consolidated MTO Generator.
/// </summary>
[ApiController]
[Route("api")]
public sealed class ProjectRevisionsController : ControllerBase
{
    private readonly IProjectRevisionService _revisionService;
    private readonly IProjectLineService _lineService;
    private readonly IMtoService _mtoService;

    public ProjectRevisionsController(IProjectRevisionService revisionService, IProjectLineService lineService, IMtoService mtoService)
    {
        _revisionService = revisionService;
        _lineService = lineService;
        _mtoService = mtoService;
    }

    [HttpGet("projects/{projectId:guid}/revisions")]
    [Authorize(Policy = PolicyNames.ViewOnly)]
    public async Task<ActionResult<IReadOnlyList<RevisionListItemDto>>> ListForProject(Guid projectId, CancellationToken ct) =>
        Ok(await _revisionService.ListAsync(projectId, ct));

    [HttpGet("project-revisions/{revisionId:guid}")]
    [Authorize(Policy = PolicyNames.ViewOnly)]
    public async Task<ActionResult<ProjectRevisionDto>> Get(Guid revisionId, CancellationToken ct) =>
        Ok(await _revisionService.GetAsync(revisionId, ct));

    [HttpPost("projects/{projectId:guid}/revisions/create-using-latest-prices")]
    [Authorize(Policy = PolicyNames.ManageProjects)]
    public async Task<ActionResult<ProjectRevisionDto>> CreateNewRevisionUsingLatestPrices(Guid projectId, CancellationToken ct) =>
        Ok(await _revisionService.CreateNewRevisionUsingLatestPricesAsync(projectId, ct));

    [HttpGet("project-revisions/compare")]
    [Authorize(Policy = PolicyNames.ViewOnly)]
    public async Task<ActionResult<RevisionComparisonDto>> Compare([FromQuery] Guid fromRevisionId, [FromQuery] Guid toRevisionId, CancellationToken ct) =>
        Ok(await _revisionService.CompareAsync(fromRevisionId, toRevisionId, ct));

    // --- Lineup / BOM generator ---

    [HttpPost("project-revisions/{revisionId:guid}/lines")]
    [Authorize(Policy = PolicyNames.ManageProjects)]
    public async Task<ActionResult<ProjectRevisionDto>> AddLine(Guid revisionId, AddProjectLineRequest request, CancellationToken ct) =>
        Ok(await _lineService.AddLineAsync(revisionId, request, ct));

    [HttpPut("project-revisions/{revisionId:guid}/lines/{lineId:guid}/quantity")]
    [Authorize(Policy = PolicyNames.ManageProjects)]
    public async Task<ActionResult<ProjectRevisionDto>> UpdateLineQuantity(
        Guid revisionId, Guid lineId, UpdateProjectLineQuantityRequest request, CancellationToken ct) =>
        Ok(await _lineService.UpdateLineQuantityAsync(revisionId, lineId, request, ct));

    [HttpDelete("project-revisions/{revisionId:guid}/lines/{lineId:guid}")]
    [Authorize(Policy = PolicyNames.ManageProjects)]
    public async Task<ActionResult<ProjectRevisionDto>> RemoveLine(
        Guid revisionId, Guid lineId, [FromBody] byte[] rowVersion, CancellationToken ct) =>
        Ok(await _lineService.RemoveLineAsync(revisionId, lineId, rowVersion, ct));

    [HttpPost("project-revisions/{revisionId:guid}/lines/{lineId:guid}/override")]
    [Authorize(Policy = PolicyNames.ManageProjects)]
    public async Task<ActionResult<ProjectRevisionDto>> OverrideLineField(
        Guid revisionId, Guid lineId, ProjectLineOverrideRequest request, CancellationToken ct) =>
        Ok(await _lineService.OverrideLineFieldAsync(revisionId, lineId, request, ct));

    [HttpGet("project-lines/{lineId:guid}/override-history")]
    [Authorize(Policy = PolicyNames.ViewOnly)]
    public async Task<ActionResult<IReadOnlyList<ProjectLineOverrideHistoryDto>>> OverrideHistory(Guid lineId, CancellationToken ct) =>
        Ok(await _lineService.GetOverrideHistoryAsync(lineId, ct));

    // --- MTO (Section 16) ---

    [HttpGet("project-revisions/{revisionId:guid}/mto")]
    [Authorize(Policy = PolicyNames.ViewOnly)]
    public async Task<ActionResult<MtoResultDto>> GetMto(Guid revisionId, CancellationToken ct) =>
        Ok(await _mtoService.GetMtoAsync(revisionId, ct));

    // --- Approval workflow (Section 6/21) ---

    [HttpPost("project-revisions/{revisionId:guid}/submit")]
    [Authorize(Policy = PolicyNames.ManageProjects)]
    public async Task<ActionResult<ProjectRevisionDto>> SubmitForApproval(Guid revisionId, SubmitForApprovalRequest request, CancellationToken ct) =>
        Ok(await _revisionService.SubmitForApprovalAsync(revisionId, request, ct));

    [HttpPost("project-revisions/{revisionId:guid}/decide")]
    [Authorize(Policy = PolicyNames.Approve)]
    public async Task<ActionResult<ProjectRevisionDto>> DecideApproval(Guid revisionId, ApprovalDecisionRequest request, CancellationToken ct) =>
        Ok(await _revisionService.DecideApprovalAsync(revisionId, request, ct));

    [HttpPost("project-revisions/{revisionId:guid}/lock")]
    [Authorize(Policy = PolicyNames.Approve)]
    public async Task<ActionResult<ProjectRevisionDto>> Lock(Guid revisionId, LockRevisionRequest request, CancellationToken ct) =>
        Ok(await _revisionService.LockAsync(revisionId, request, ct));

    [HttpGet("project-revisions/{revisionId:guid}/approval-history")]
    [Authorize(Policy = PolicyNames.ViewOnly)]
    public async Task<ActionResult<IReadOnlyList<ApprovalHistoryItemDto>>> ApprovalHistory(Guid revisionId, CancellationToken ct) =>
        Ok(await _revisionService.GetApprovalHistoryAsync(revisionId, ct));
}
