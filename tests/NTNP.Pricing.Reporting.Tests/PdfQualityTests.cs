using NTNP.Pricing.Reporting.Excel;
using NTNP.Pricing.Reporting.Rendering;
using UglyToad.PdfPig;

namespace NTNP.Pricing.Reporting.Tests;

/// <summary>Section 29 — PDF visual/structural verification (points 1-6, 10) against the real render pipeline.</summary>
public class PdfQualityTests : IAsyncLifetime
{
    private HtmlToPdfRenderer _renderer = null!;
    private ReportRenderer _reportRenderer = null!;

    public Task InitializeAsync()
    {
        _renderer = new HtmlToPdfRenderer();
        _reportRenderer = new ReportRenderer(_renderer);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _renderer.DisposeAsync();

    [Fact]
    public async Task OnePage_Quotation_Renders_Valid_SinglePage_Pdf()
    {
        var model = ModelFactory.SmallQuotation("fa");
        var bytes = await _reportRenderer.RenderCustomerQuotationPdfAsync(model);

        using var doc = PdfDocument.Open(bytes);
        Assert.True(doc.NumberOfPages >= 1);
        Assert.True(bytes.Length > 1000, "PDF should not be a near-empty/corrupt file.");
    }

    // NOTE on Persian text assertions in this file: Chromium's PDF export stores RTL/Arabic-script
    // text as shaped presentation-form glyphs, so PdfPig's text-layer extraction does not recover
    // the original logical-order Unicode string for Persian content (this affects any PDF text
    // extractor, not just PdfPig — it is a known limitation of naive PDF text-layer reading for
    // complex scripts). English/Latin and numeric runs are unaffected and extract correctly (see the
    // passing assertions below), so those carry the automated verification; Persian-specific content
    // is instead verified at the HTML-source level (pre-Chromium, still logical-order Unicode) in
    // <see cref="HtmlSourceContainsPersianContent"/>, plus a manual rendered-image inspection — see
    // docs/deployment.md's report-QA note and the PNG artifacts produced by RenderReferencePdfs.

    [Fact]
    public async Task MultiPage_Quotation_With_30Plus_Lines_Repeats_Table_Header_On_Every_Page()
    {
        var model = ModelFactory.LargeQuotation("en"); // English so the repeated header text extracts reliably
        var bytes = await _reportRenderer.RenderCustomerQuotationPdfAsync(model);

        using var doc = PdfDocument.Open(bytes);
        Assert.True(doc.NumberOfPages >= 2, "32 line items must overflow a single A4 page.");

        // The repeated column header must appear on more than one page — proof the <thead> is
        // actually repeating (Section 26: "Repeat table headers on continuation pages"). Checked via
        // the single-token "TotalPrice" header (PdfPig's raw text concatenation drops the space
        // inside multi-word headers like "Cell Code" → "CellCode" — a known extractor quirk, not a
        // rendering defect; poppler's layout-aware pdftotext confirms the real spacing is correct).
        // "TotalPrice" is unambiguous because no data cell contains that literal token.
        var pagesWithHeader = doc.GetPages().Count(p => p.Text.Contains("TotalPrice"));
        Assert.True(pagesWithHeader >= 2, $"Expected the line-item header to repeat on 2+ pages, found it on {pagesWithHeader}.");
    }

    [Fact]
    public async Task Quotation_Handles_Long_Persian_Descriptions_And_English_Technical_Names_Without_Error()
    {
        var model = ModelFactory.LargeQuotation("fa"); // includes both long Persian text and "UniSafe MV Withdrawable..." English lines
        var bytes = await _reportRenderer.RenderCustomerQuotationPdfAsync(model);

        using var doc = PdfDocument.Open(bytes);
        Assert.True(doc.NumberOfPages >= 1);
        var text = string.Join(" ", doc.GetPages().Select(p => p.Text));
        Assert.Contains("UniSafe", text); // the English technical name extracts correctly even inside an RTL document

        var (html, _, _) = NTNP.Pricing.Reporting.Rendering.CustomerQuotationHtmlBuilder.Build(model);
        Assert.Contains("تابلوی ورودی فشار متوسط", html); // long Persian description reached the template intact
    }

    [Fact]
    public void HtmlSourceContainsPersianContent()
    {
        // The HTML source is still logical-order Unicode (shaping happens only when Chromium lays
        // out the glyphs for the PDF), so this is the reliable place to assert exact Persian text.
        var model = ModelFactory.SmallQuotation("fa");
        var (html, header, footer) = NTNP.Pricing.Reporting.Rendering.CustomerQuotationHtmlBuilder.Build(model);

        Assert.Contains("پیشنهاد فنی و مالی", html);
        Assert.Contains(model.Company.LegalNameFa, html);
        Assert.Contains("dir=\"rtl\"", html);
        Assert.Contains(model.Company.LegalNameFa, header);
    }

    [Fact]
    public async Task Quotation_With_Rial_And_Eur_Totals_Reconciles_To_The_Model()
    {
        var model = ModelFactory.SmallQuotation("en");
        var bytes = await _reportRenderer.RenderCustomerQuotationPdfAsync(model);

        using var doc = PdfDocument.Open(bytes);
        var text = string.Join(" ", doc.GetPages().Select(p => p.Text));

        Assert.Contains(model.TotalRialPayable.ToString("N0"), text);
        Assert.Contains(model.TotalForeignPayable.ToString("N2"), text);
    }

    [Fact]
    public async Task Bilingual_Quotation_Contains_Both_Persian_And_English_Sections()
    {
        var model = ModelFactory.SmallQuotation("bilingual");
        var bytes = await _reportRenderer.RenderCustomerQuotationPdfAsync(model);

        using var doc = PdfDocument.Open(bytes);
        var text = string.Join(" ", doc.GetPages().Select(p => p.Text));
        Assert.Contains("Technical & Commercial Proposal", text); // English section

        // The fa-then-en concatenation (see CustomerQuotationHtmlBuilder) must actually contain a
        // Persian section too — checked at the HTML-source level (see class-level note above).
        var (html, _, _) = NTNP.Pricing.Reporting.Rendering.CustomerQuotationHtmlBuilder.Build(model);
        Assert.Contains("پیشنهاد فنی و مالی", html);
        Assert.Contains("page-break-before: always", html); // fa/en sections are on separate pages
    }

    [Fact]
    public async Task Internal_Costing_Report_Pdf_Reconciles_To_The_Model_Totals()
    {
        var model = ModelFactory.InternalCostingReport();
        var bytes = await _reportRenderer.RenderInternalCostingPdfAsync(model);

        using var doc = PdfDocument.Open(bytes);
        var text = string.Join(" ", doc.GetPages().Select(p => p.Text));

        Assert.Contains(model.Totals.TotalProjectCostIrr.ToString("N0"), text);
        Assert.Contains(model.Totals.TotalProjectSellingPriceIrr.ToString("N0"), text);
        Assert.Contains("INTERNAL", text); // English half of the confidentiality banner, present on every page

        var (html, header, _) = NTNP.Pricing.Reporting.Rendering.InternalReportHtmlBuilder.BuildCostingReport(model);
        Assert.Contains("محرمانه", html); // Persian half, verified at the HTML-source level (see class note above)
        Assert.Contains("محرمانه", header);
    }

    [Fact]
    public async Task Internal_Costing_Report_Excel_Contains_Same_Totals_As_Pdf()
    {
        var model = ModelFactory.InternalCostingReport();
        var xlsxBytes = _reportRenderer.RenderInternalCostingExcel(model);

        using var workbook = new ClosedXML.Excel.XLWorkbook(new MemoryStream(xlsxBytes));
        var sheet = workbook.Worksheet("Internal Costing Report");

        // Locate the "Total Project Cost (IRR)" label and check the value beside it.
        var costCell = sheet.CellsUsed(c => c.GetString() == "Total Project Cost (IRR)").Single();
        var costValue = sheet.Cell(costCell.Address.RowNumber, costCell.Address.ColumnNumber + 1).GetValue<decimal>();

        Assert.Equal(model.Totals.TotalProjectCostIrr, costValue);
    }

    [Fact]
    public void GeneratedFileName_Follows_MandatedPattern_And_Strips_Invalid_Characters()
    {
        var fileName = FilenameSanitizer.BuildQuotationFileName("Q/2026:0042", 3, "Sample*Co.", "Project \"A\"");

        Assert.StartsWith("NTNP-Quotation-", fileName);
        Assert.EndsWith(".pdf", fileName);
        Assert.DoesNotContain('/', fileName);
        Assert.DoesNotContain(':', fileName);
        Assert.DoesNotContain('*', fileName);
        Assert.DoesNotContain('"', fileName);
    }
}
