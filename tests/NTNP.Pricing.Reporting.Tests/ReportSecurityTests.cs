using NTNP.Pricing.Reporting.Rendering;
using UglyToad.PdfPig;

namespace NTNP.Pricing.Reporting.Tests;

/// <summary>
/// Section 29 — automated proof that customer reports never contain internal fields or labels, in
/// both Persian and English output. Uses the real render pipeline (headless Chromium via
/// Playwright) and extracts the PDF's actual text layer with PdfPig, so this is a genuine
/// end-to-end check of the shipped artifact, not a check on the input model alone.
/// </summary>
public class ReportSecurityTests : IAsyncLifetime
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

    // English terms that must never appear in the customer-facing PDF (Section 29's explicit list).
    private static readonly string[] BannedEnglishTerms =
    {
        "Purchase Price", "Equipment Cost", "BODY+ES Cost", "Markup", "Gross Margin", "Profit",
        "Supplier", "Purchase Exchange Rate", "Internal Notes", "Override Reason",
    };

    // Persian equivalents actually used by the internal report templates — must not leak either.
    private static readonly string[] BannedPersianTerms =
    {
        "قیمت خرید", "هزینه تجهیزات", "هزینه بدنه", "سود", "حاشیه سود", "تامین‌کننده", "نرخ ارز خرید",
    };

    [Theory]
    [InlineData("fa")]
    [InlineData("en")]
    public async Task CustomerQuotation_Pdf_Never_Contains_Internal_Cost_Terms(string language)
    {
        var model = ModelFactory.SmallQuotation(language);
        var pdfBytes = await _reportRenderer.RenderCustomerQuotationPdfAsync(model);

        // PDF text-layer extraction (reliable for the English/Latin terms Section 29 lists — RTL
        // Persian text is checked separately below at the HTML-source level; see the class note on
        // CustomerQuotation_Html_Never_Contains_Internal_Cost_Terms for why).
        var text = ExtractAllText(pdfBytes);
        foreach (var term in BannedEnglishTerms)
            Assert.DoesNotContain(term, text, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("fa")]
    [InlineData("en")]
    [InlineData("bilingual")]
    public void CustomerQuotation_Html_Never_Contains_Internal_Cost_Terms(string language)
    {
        // Chromium's PDF export stores RTL/Arabic-script text as shaped presentation-form glyphs,
        // so a PDF text-layer extractor cannot recover the logical-order Persian string to compare
        // against a banned term (this affects any extractor, not just the one used above — a known
        // limitation for complex scripts). The HTML source fed into Chromium is still logical-order
        // Unicode, so it is the reliable place to prove a Persian banned term never reached the
        // template; it is exactly the same string a leaked field would have to pass through.
        var model = ModelFactory.SmallQuotation(language);
        var (html, header, footer) = CustomerQuotationHtmlBuilder.Build(model);
        var combined = html + header + footer;

        foreach (var term in BannedEnglishTerms)
            Assert.DoesNotContain(term, combined, StringComparison.OrdinalIgnoreCase);
        foreach (var term in BannedPersianTerms)
            Assert.DoesNotContain(term, combined, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CustomerQuotation_Pdf_Contains_Only_SellingPrice_Never_Cost()
    {
        var model = ModelFactory.SmallQuotation("en");
        var pdfBytes = await _reportRenderer.RenderCustomerQuotationPdfAsync(model);
        var text = ExtractAllText(pdfBytes);

        // Positive control: the total foreign payable (a selling-side figure) IS expected to appear.
        Assert.Contains(model.TotalForeignPayable.ToString("N2"), text);
    }

    [Fact]
    public void CustomerQuotationModel_Type_Has_No_Cost_Or_Margin_Property()
    {
        // Defense in depth: even if a future edit tried to print a new field, there is no property
        // on the model to source internal data from in the first place.
        var forbiddenSubstrings = new[] { "cost", "margin", "profit", "markup", "supplier", "purchaserate", "internalnote", "overridereason" };

        void AssertNoForbiddenProperties(Type type, HashSet<Type> visited)
        {
            if (!visited.Add(type)) return;
            foreach (var prop in type.GetProperties())
            {
                var normalized = prop.Name.Replace("_", "").ToLowerInvariant();
                Assert.DoesNotContain(forbiddenSubstrings, f => normalized.Contains(f));

                var propType = prop.PropertyType;
                if (propType.Namespace?.StartsWith("NTNP.Pricing.Reporting.Models") == true)
                    AssertNoForbiddenProperties(propType, visited);
                if (propType.IsGenericType && propType.GetGenericArguments().Length == 1 &&
                    propType.GetGenericArguments()[0].Namespace?.StartsWith("NTNP.Pricing.Reporting.Models") == true)
                    AssertNoForbiddenProperties(propType.GetGenericArguments()[0], visited);
            }
        }

        AssertNoForbiddenProperties(typeof(Models.CustomerQuotationModel), new HashSet<Type>());
    }

    private static string ExtractAllText(byte[] pdfBytes)
    {
        using var document = PdfDocument.Open(pdfBytes);
        return string.Join("\n", document.GetPages().Select(p => p.Text));
    }
}
