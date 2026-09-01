using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTNP.Pricing.Api.Authorization;
using NTNP.Pricing.Application.Common;
using NTNP.Pricing.Application.Files;
using NTNP.Pricing.Contracts.Files;

namespace NTNP.Pricing.Api.Controllers;

/// <summary>
/// Section 32 — read/download access to server-controlled stored files (generated reports,
/// imported Excel workbooks, project attachments). Files are never served from the desktop
/// client's local disk; every download goes through this authorized endpoint.
/// </summary>
[ApiController]
[Route("api/files")]
public sealed class FilesController : ControllerBase
{
    private readonly IFileService _files;
    private readonly IFileStorageService _fileStorage;

    public FilesController(IFileService files, IFileStorageService fileStorage)
    {
        _files = files;
        _fileStorage = fileStorage;
    }

    [HttpGet]
    [Authorize(Policy = PolicyNames.ViewOnly)]
    public async Task<ActionResult<IReadOnlyList<StoredFileDto>>> List(
        [FromQuery] Guid? projectId, [FromQuery] Guid? projectRevisionId, [FromQuery] string? category, CancellationToken ct) =>
        Ok(await _files.ListAsync(projectId, projectRevisionId, category, ct));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = PolicyNames.ViewOnly)]
    public async Task<ActionResult<StoredFileDto>> Get(Guid id, CancellationToken ct) => Ok(await _files.GetAsync(id, ct));

    [HttpGet("{id:guid}/download")]
    [Authorize(Policy = PolicyNames.ViewOnly)]
    public async Task<IActionResult> Download(Guid id, CancellationToken ct)
    {
        var meta = await _files.GetAsync(id, ct);
        var storagePath = await _files.GetStoragePathAsync(id, ct);
        var stream = await _fileStorage.OpenReadAsync(storagePath, ct);
        return File(stream, meta.ContentType, meta.FileName);
    }
}
