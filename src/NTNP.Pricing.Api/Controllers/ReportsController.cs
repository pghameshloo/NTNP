using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTNP.Pricing.Api.Authorization;
using NTNP.Pricing.Api.Reports;
using NTNP.Pricing.Application.Common;
using NTNP.Pricing.Application.Files;
using NTNP.Pricing.Application.Projects;
using NTNP.Pricing.Application.Settings;
using NTNP.Pricing.Contracts.Mto;
using NTNP.Pricing.Contracts.Settings;
using NTNP.Pricing.Domain.Enums;
using NTNP.Pricing.Reporting;

namespace NTNP.Pricing.Api.Controllers;

/// <summary>
/// Section 26 (customer quotation), Section 16 (BOM/MTO), Section 19/21 (internal costing +
/// revision comparison). Every generated document is registered as a <see cref="Domain.Entities.StoredFile"/>
/// (Section 32) and audited as <see cref="AuditAction.ReportIssued"/> (Section 30) before being
/// streamed back to the caller.
/// </summary>
[ApiController]
[Route("api")]
public sealed class ReportsController : ControllerBase
{
    private const string PdfContentType = "application/pdf";
    private const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private readonly IProjectService _projects;
    private readonly IProjectRevisionService _revisions;
    private readonly IMtoService _mto;
    private readonly ICompanySettingsService _settings;
    private readonly IReportRenderer _renderer;
    private readonly IFileStorageService _fileStorage;
    private readonly IFileService _files;
    private readonly IAuditService _audit;

    public ReportsController(
        IProjectService projects, IProjectRevisionService revisions, IMtoService mto, ICompanySettingsService settings,
        IReportRenderer renderer, IFileStorageService fileStorage, IFileService files, IAuditService audit)
    {
        _projects = projects;
        _revisions = revisions;
        _mto = mto;
        _settings = settings;
        _renderer = renderer;
        _fileStorage = fileStorage;
        _files = files;
        _audit = audit;
    }

    /// <summary>Section 26 — the customer-facing quotation. <paramref name="language"/> is "fa", "en" or "bilingual".</summary>
    [HttpGet("project-revisions/{revisionId:guid}/reports/quotation")]
    [Authorize(Policy = PolicyNames.ViewOnly)]
    public async Task<IActionResult> GetQuotation(Guid revisionId, [FromQuery] string language = "bilingual", CancellationToken ct = default)
    {
        var revision = await _revisions.GetAsync(revisionId, ct);
        var project = await _projects.GetAsync(revision.ProjectId, ct);
        var settings = await _settings.GetAsync(ct);
        var logo = await LoadLogoBytesAsync(settings, ct);

        var model = ReportModelMapper.ToCustomerQuotationModel(project, revision, settings, logo, NormalizeLanguage(language));
        var pdf = await _renderer.RenderCustomerQuotationPdfAsync(model, ct);

        var fileName = FilenameSanitizer.BuildQuotationFileName(
            project.QuotationNumber ?? project.ProjectCode, revision.RevisionNumber, project.CustomerName, project.ProjectName);

        await PersistAndAuditAsync(pdf, fileName, PdfContentType, FileCategory.GeneratedQuotation, project.Id, revision.Id, ct);
        return File(pdf, PdfContentType, fileName);
    }

    /// <summary>Section 19 — the internal costing report, cost/margin fields included by design (never sent to a customer).</summary>
    [HttpGet("project-revisions/{revisionId:guid}/reports/internal-costing")]
    [Authorize(Policy = PolicyNames.ViewOnly)]
    public async Task<IActionResult> GetInternalCosting(Guid revisionId, [FromQuery] string format = "pdf", CancellationToken ct = default)
    {
        var revision = await _revisions.GetAsync(revisionId, ct);
        var project = await _projects.GetAsync(revision.ProjectId, ct);
        var settings = await _settings.GetAsync(ct);
        var logo = await LoadLogoBytesAsync(settings, ct);

        var model = ReportModelMapper.ToInternalCostingReportModel(project, revision, settings, logo, User.Identity?.Name ?? "system");
        var fileNameBase = FilenameSanitizer.Sanitize($"NTNP-InternalCosting-{project.ProjectCode}-Rev-{revision.RevisionNumber}");

        if (string.Equals(format, "xlsx", StringComparison.OrdinalIgnoreCase))
        {
            var xlsx = _renderer.RenderInternalCostingExcel(model);
            var fileName = $"{fileNameBase}.xlsx";
            await PersistAndAuditAsync(xlsx, fileName, XlsxContentType, FileCategory.InternalReport, project.Id, revision.Id, ct);
            return File(xlsx, XlsxContentType, fileName);
        }

        var pdf = await _renderer.RenderInternalCostingPdfAsync(model, ct);
        var pdfFileName = $"{fileNameBase}.pdf";
        await PersistAndAuditAsync(pdf, pdfFileName, PdfContentType, FileCategory.InternalReport, project.Id, revision.Id, ct);
        return File(pdf, PdfContentType, pdfFileName);
    }

    /// <summary>Section 16 — Automatic Consolidated MTO Generator. <paramref name="kind"/> is "electrical", "bodyes" or "combined".</summary>
    [HttpGet("project-revisions/{revisionId:guid}/reports/mto")]
    [Authorize(Policy = PolicyNames.ViewOnly)]
    public async Task<IActionResult> GetMto(Guid revisionId, [FromQuery] string kind = "combined", [FromQuery] string format = "pdf", CancellationToken ct = default)
    {
        var revision = await _revisions.GetAsync(revisionId, ct);
        var project = await _projects.GetAsync(revision.ProjectId, ct);
        var settings = await _settings.GetAsync(ct);
        var logo = await LoadLogoBytesAsync(settings, ct);
        var mto = await _mto.GetMtoAsync(revisionId, ct);

        var (rows, title, tag) = kind.ToLowerInvariant() switch
        {
            "electrical" => (mto.Electrical, "Electrical BOM / Material Take-Off", "Electrical"),
            "bodyes" => (mto.BodyEs, "Body & Electrical Shop (BODY+ES) Material Take-Off", "BodyEs"),
            "combined" => (mto.Combined, "Consolidated Material Take-Off (MTO)", "Combined"),
            _ => throw new NTNP.Pricing.Domain.Exceptions.DomainValidationException(new[] { $"Unknown MTO kind '{kind}'. Use electrical, bodyes or combined." }),
        };

        var model = ReportModelMapper.ToBomMtoReportModel(project, revision, settings, logo, rows, title);
        var fileNameBase = FilenameSanitizer.Sanitize($"NTNP-MTO-{tag}-{project.ProjectCode}-Rev-{revision.RevisionNumber}");

        if (string.Equals(format, "xlsx", StringComparison.OrdinalIgnoreCase))
        {
            var xlsx = _renderer.RenderBomMtoExcel(model);
            var fileName = $"{fileNameBase}.xlsx";
            await PersistAndAuditAsync(xlsx, fileName, XlsxContentType, FileCategory.BomMtoExport, project.Id, revision.Id, ct);
            return File(xlsx, XlsxContentType, fileName);
        }

        var pdf = await _renderer.RenderBomMtoPdfAsync(model, ct);
        var pdfFileName = $"{fileNameBase}.pdf";
        await PersistAndAuditAsync(pdf, pdfFileName, PdfContentType, FileCategory.BomMtoExport, project.Id, revision.Id, ct);
        return File(pdf, PdfContentType, pdfFileName);
    }

    /// <summary>Section 21 — side-by-side revision comparison (cost/price/profit/margin deltas plus changed fields).</summary>
    [HttpGet("project-revisions/compare/report")]
    [Authorize(Policy = PolicyNames.ViewOnly)]
    public async Task<IActionResult> GetRevisionComparison(
        [FromQuery] Guid fromRevisionId, [FromQuery] Guid toRevisionId, [FromQuery] string format = "pdf", CancellationToken ct = default)
    {
        var toRevision = await _revisions.GetAsync(toRevisionId, ct);
        var project = await _projects.GetAsync(toRevision.ProjectId, ct);
        var settings = await _settings.GetAsync(ct);
        var logo = await LoadLogoBytesAsync(settings, ct);
        var comparison = await _revisions.CompareAsync(fromRevisionId, toRevisionId, ct);

        var model = ReportModelMapper.ToRevisionComparisonReportModel(project, comparison, settings, logo);
        var fileNameBase = FilenameSanitizer.Sanitize(
            $"NTNP-RevisionComparison-{project.ProjectCode}-Rev{comparison.FromRevisionNumber}-vs-Rev{comparison.ToRevisionNumber}");

        if (string.Equals(format, "xlsx", StringComparison.OrdinalIgnoreCase))
        {
            var xlsx = _renderer.RenderRevisionComparisonExcel(model);
            var fileName = $"{fileNameBase}.xlsx";
            await PersistAndAuditAsync(xlsx, fileName, XlsxContentType, FileCategory.InternalReport, project.Id, toRevision.Id, ct);
            return File(xlsx, XlsxContentType, fileName);
        }

        var pdf = await _renderer.RenderRevisionComparisonPdfAsync(model, ct);
        var pdfFileName = $"{fileNameBase}.pdf";
        await PersistAndAuditAsync(pdf, pdfFileName, PdfContentType, FileCategory.InternalReport, project.Id, toRevision.Id, ct);
        return File(pdf, PdfContentType, pdfFileName);
    }

    private static string NormalizeLanguage(string language) => language.ToLowerInvariant() switch
    {
        "fa" => "fa",
        "en" => "en",
        _ => "bilingual",
    };

    private async Task<byte[]?> LoadLogoBytesAsync(CompanySettingsDto settings, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(settings.LogoStoragePath))
            return null;

        try
        {
            await using var stream = await _fileStorage.OpenReadAsync(settings.LogoStoragePath, ct);
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, ct);
            return buffer.ToArray();
        }
        catch (IOException)
        {
            // Section 10 (ASSUMPTIONS.md): a missing/renamed logo file must never break report
            // generation — the report simply renders without the letterhead image.
            return null;
        }
    }

    /// <summary>Registers the generated document as a <see cref="Domain.Entities.StoredFile"/> (Section 32) and audits its issuance (Section 30).</summary>
    private async Task PersistAndAuditAsync(
        byte[] content, string fileName, string contentType, FileCategory category, Guid projectId, Guid revisionId, CancellationToken ct)
    {
        var saved = await _fileStorage.SaveAsync(fileName, new MemoryStream(content), ct);
        var registered = await _files.RegisterAsync(saved, fileName, contentType, category, projectId, revisionId, ct);

        await _audit.LogAsync(
            AuditAction.ReportIssued,
            nameof(Domain.Entities.StoredFile),
            registered.Id.ToString(),
            oldValue: null,
            newValue: new { fileName, contentType, category = category.ToString(), saved.SizeBytes, saved.Sha256Hash },
            reason: null,
            projectId: projectId,
            projectRevisionId: revisionId,
            cancellationToken: ct);
    }
}
