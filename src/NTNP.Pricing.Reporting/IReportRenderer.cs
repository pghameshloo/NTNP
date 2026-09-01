using NTNP.Pricing.Reporting.Excel;
using NTNP.Pricing.Reporting.Models;
using NTNP.Pricing.Reporting.Rendering;

namespace NTNP.Pricing.Reporting;

/// <summary>The single entry point the Api layer calls to turn a report model into bytes.</summary>
public interface IReportRenderer
{
    Task<byte[]> RenderCustomerQuotationPdfAsync(CustomerQuotationModel model, CancellationToken ct = default);
    Task<byte[]> RenderInternalCostingPdfAsync(InternalCostingReportModel model, CancellationToken ct = default);
    Task<byte[]> RenderBomMtoPdfAsync(BomMtoReportModel model, CancellationToken ct = default);
    Task<byte[]> RenderRevisionComparisonPdfAsync(RevisionComparisonReportModel model, CancellationToken ct = default);

    byte[] RenderBomMtoExcel(BomMtoReportModel model);
    byte[] RenderInternalCostingExcel(InternalCostingReportModel model);
    byte[] RenderRevisionComparisonExcel(RevisionComparisonReportModel model);
}

public sealed class ReportRenderer : IReportRenderer
{
    private readonly HtmlToPdfRenderer _pdfRenderer;

    public ReportRenderer(HtmlToPdfRenderer pdfRenderer) => _pdfRenderer = pdfRenderer;

    public Task<byte[]> RenderCustomerQuotationPdfAsync(CustomerQuotationModel model, CancellationToken ct = default)
    {
        var (body, header, footer) = CustomerQuotationHtmlBuilder.Build(model);
        return _pdfRenderer.RenderPdfAsync(body, new PdfRenderOptions(header, footer), ct);
    }

    public Task<byte[]> RenderInternalCostingPdfAsync(InternalCostingReportModel model, CancellationToken ct = default)
    {
        var (body, header, footer) = InternalReportHtmlBuilder.BuildCostingReport(model);
        return _pdfRenderer.RenderPdfAsync(body, WideTablePdfOptions(header, footer), ct);
    }

    public Task<byte[]> RenderBomMtoPdfAsync(BomMtoReportModel model, CancellationToken ct = default)
    {
        var (body, header, footer) = InternalReportHtmlBuilder.BuildBomMtoReport(model);
        return _pdfRenderer.RenderPdfAsync(body, WideTablePdfOptions(header, footer), ct);
    }

    public Task<byte[]> RenderRevisionComparisonPdfAsync(RevisionComparisonReportModel model, CancellationToken ct = default)
    {
        var (body, header, footer) = InternalReportHtmlBuilder.BuildRevisionComparison(model);
        return _pdfRenderer.RenderPdfAsync(body, new PdfRenderOptions(header, footer), ct);
    }

    public byte[] RenderBomMtoExcel(BomMtoReportModel model) => ExcelReportBuilder.BuildBomMtoWorkbook(model);
    public byte[] RenderInternalCostingExcel(InternalCostingReportModel model) => ExcelReportBuilder.BuildInternalCostingWorkbook(model);
    public byte[] RenderRevisionComparisonExcel(RevisionComparisonReportModel model) => ExcelReportBuilder.BuildRevisionComparisonWorkbook(model);

    /// <summary>
    /// The Internal Costing Report and combined MTO tables run to 20+ columns — landscape A4 with
    /// the default 12mm side margins still clips the outermost column (verified via rendered-page
    /// inspection). Narrower margins reclaim the width those reports need.
    /// </summary>
    private static PdfRenderOptions WideTablePdfOptions(string header, string footer) =>
        new(header, footer, LeftMargin: "6mm", RightMargin: "6mm", Landscape: true);
}
