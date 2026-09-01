using System.Globalization;
using System.Net;
using System.Text;
using NTNP.Pricing.Reporting.Assets;
using NTNP.Pricing.Reporting.Models;

namespace NTNP.Pricing.Reporting.Rendering;

/// <summary>
/// Section 26 — "NTNP Premium Corporate" customer quotation template. Builds a fully self-contained
/// HTML document (fonts/logo inlined, no external requests) plus separate Playwright header/footer
/// templates, from a <see cref="CustomerQuotationModel"/> only — the type system itself makes it
/// impossible for this class to render an internal cost figure, since no such field exists on the
/// model (Section 26: "never expose... never generate by merely hiding columns").
/// </summary>
public static class CustomerQuotationHtmlBuilder
{
    public static (string Body, string Header, string Footer) Build(CustomerQuotationModel model)
    {
        var isFa = model.LanguageCode != "en";
        var isBilingual = model.LanguageCode == "bilingual";
        // Each section is wrapped in its own dir/lang container (rather than relying solely on the
        // <html> root) so a bilingual document's English section is genuinely LTR even though the
        // document root defaults to RTL for the (primary) Persian section.
        var faBody = $"<div dir=\"rtl\" lang=\"fa\">{BuildLanguageSection(model, fa: true, useLabelTitle: isBilingual)}</div>";
        var enBody = $"<div dir=\"ltr\" lang=\"en\">{BuildLanguageSection(model, fa: false, useLabelTitle: isBilingual)}</div>";

        var body = model.LanguageCode switch
        {
            "fa" => faBody,
            "en" => enBody,
            _ => faBody + "<div style='page-break-before: always;'></div>" + enBody, // bilingual: fa pages, then en pages
        };

        var doc = $$"""
            <!DOCTYPE html>
            <html dir="{{(isFa ? "rtl" : "ltr")}}" lang="{{(isFa ? "fa" : "en")}}">
            <head>
            <meta charset="utf-8" />
            <style>{{SharedCss()}}</style>
            </head>
            <body>{{body}}</body>
            </html>
            """;

        var header = BuildHeaderTemplate(model, isFa);
        var footer = BuildFooterTemplate(model, isFa);
        return (doc, header, footer);
    }

    private static string BuildLanguageSection(CustomerQuotationModel m, bool fa, bool useLabelTitle)
    {
        var t = Labels.For(fa);
        // A bilingual document renders both sections from one shared model.QuotationTitle field, so
        // each section instead uses its language's standard title (Labels.QuotationTitle); a
        // single-language document uses the caller-provided model.QuotationTitle verbatim (Section
        // 26 allows either "پیشنهاد قیمت" or "پیشنهاد فنی و مالی" wording, so this stays customizable).
        var title = useLabelTitle ? t.QuotationTitle : m.QuotationTitle;
        var sb = new StringBuilder();

        // ---- First-page letterhead block ----
        sb.Append($"""
            <div class="letterhead">
              {LogoImgTag(m.Company.LogoPngBytes)}
              <div class="company-names">
                <div class="legal-name-primary">{H(fa ? m.Company.LegalNameFa : m.Company.LegalNameEn)}</div>
                <div class="legal-name-secondary">{H(fa ? m.Company.LegalNameEn : m.Company.LegalNameFa)}</div>
              </div>
            </div>
            <div class="title-block">
              <div class="doc-title">{H(title)}</div>
              <table class="meta-table">
                <tr><td>{t.QuotationNumber}</td><td>{H(m.QuotationNumber)}</td>
                    <td>{t.Revision}</td><td>{m.Revision}</td></tr>
                <tr><td>{t.IssueDate}</td><td>{m.IssueDate:yyyy-MM-dd}</td>
                    <td>{t.ValidUntil}</td><td>{(m.ValidUntil.HasValue ? m.ValidUntil.Value.ToString("yyyy-MM-dd") : "-")}</td></tr>
              </table>
            </div>
            """);

        // ---- Customer info ----
        sb.Append($"""
            <div class="section">
              <div class="section-title">{t.CustomerInformation}</div>
              <table class="meta-table">
                <tr><td>{t.CustomerCompany}</td><td dir="auto">{H(m.CustomerCompanyName)}</td>
                    <td>{t.ProjectName}</td><td dir="auto">{H(m.ProjectName)}</td></tr>
                <tr><td>{t.RfqNumber}</td><td dir="auto">{H(m.RfqNumber) ?? "-"}</td>
                    <td>{t.ContactPerson}</td><td dir="auto">{H(m.ContactPerson) ?? "-"}</td></tr>
              </table>
              {(string.IsNullOrWhiteSpace(m.AttentionLine) ? "" : $"<div class='attn'>{t.Attention}: <span dir=\"auto\">{H(m.AttentionLine)}</span></div>")}
              {(string.IsNullOrWhiteSpace(m.Subject) ? "" : $"<div class='subject'>{t.Subject}: <span dir=\"auto\">{H(m.Subject)}</span></div>")}
            </div>
            """);

        // ---- Commercial summary cards ----
        sb.Append($"""
            <div class="section">
              <div class="section-title">{t.CommercialSummary}</div>
              <div class="summary-cards">
                <div class="card"><div class="card-label">{t.TotalRialPayable}</div><div class="card-value">{FormatIrr(m.TotalRialPayable)}</div></div>
                <div class="card"><div class="card-label">{t.TotalForeignPayable}</div><div class="card-value">{FormatForeign(m.TotalForeignPayable)} {H(m.QuotationCurrencyCode)}</div></div>
                <div class="card"><div class="card-label">{t.QuotationCurrency}</div><div class="card-value">{H(m.QuotationCurrencyCode)}</div></div>
                <div class="card"><div class="card-label">{t.Validity}</div><div class="card-value">{(m.ValidUntil.HasValue ? m.ValidUntil.Value.ToString("yyyy-MM-dd") : "-")}</div></div>
              </div>
              {(string.IsNullOrWhiteSpace(m.SellingRateBasisNote) ? "" : $"<div class='note'>{t.SellingRateBasis}: {H(m.SellingRateBasisNote)}</div>")}
            </div>
            """);

        // ---- Line items table ----
        sb.Append($"""
            <div class="section">
              <div class="section-title">{t.LineItems}</div>
              <table class="items-table">
                <thead><tr>
                  <th>{t.Row}</th><th>{t.CellCode}</th><th>{t.PanelDescription}</th><th>{t.ProductFamily}</th>
                  <th>{t.VoltageLevel}</th><th>{t.Quantity}</th><th>{t.Unit}</th>
                  <th>{t.UnitPrice}</th><th>{t.TotalPrice}</th><th>{t.Currency}</th>
                </tr></thead>
                <tbody>
            """);
        foreach (var line in m.Lines)
        {
            sb.Append($"""
                <tr>
                  <td class="num">{line.Row}</td><td>{H(line.CellCode)}</td><td class="desc" dir="auto">{H(line.PanelDescription)}</td>
                  <td dir="auto">{H(line.ProductFamily)}</td><td>{H(line.VoltageLevel) ?? "-"}</td>
                  <td class="num">{line.Quantity:0.##}</td><td>{H(line.Unit)}</td>
                  <td class="num">{FormatForeign(line.UnitSellingPrice)}</td><td class="num">{FormatForeign(line.TotalLinePrice)}</td>
                  <td>{H(line.Currency)}</td>
                </tr>
                """);
        }
        sb.Append($"""
                </tbody>
                <tfoot>
                  <tr class="grand-total"><td colspan="8">{t.GrandTotalRial}</td><td class="num" colspan="2">{FormatIrr(m.TotalRialPayable)} {t.Rial}</td></tr>
                  <tr class="grand-total"><td colspan="8">{t.GrandTotalForeign}</td><td class="num" colspan="2">{FormatForeign(m.TotalForeignPayable)} {H(m.QuotationCurrencyCode)}</td></tr>
                </tfoot>
              </table>
            </div>
            """);

        // ---- Commercial terms (only non-empty sections printed) ----
        var terms = new (string Label, string? Value)[]
        {
            (t.DeliveryTerms, m.Terms.DeliveryTerms), (t.DeliveryPeriod, m.Terms.DeliveryPeriod),
            (t.DeliveryLocation, m.Terms.DeliveryLocation), (t.PaymentTerms, m.Terms.PaymentTerms),
            (t.WarrantyTerms, m.Terms.WarrantyTerms), (t.InspectionTerms, m.Terms.InspectionTerms),
            (t.PackingTerms, m.Terms.PackingTerms), (t.TransportationTerms, m.Terms.TransportationTerms),
            (t.TaxesAndDuties, m.Terms.TaxesAndDutiesNote), (t.CurrencyBasis, m.Terms.CurrencyBasisNote),
            (t.ExchangeRateConditions, m.Terms.ExchangeRateConditionsNote), (t.ScopeExclusions, m.Terms.ScopeExclusions),
            (t.TechnicalNotes, m.Terms.TechnicalNotes), (t.CommercialNotes, m.Terms.CommercialNotes),
        };
        var printedTerms = terms.Where(x => !string.IsNullOrWhiteSpace(x.Value)).ToList();
        if (printedTerms.Count > 0)
        {
            sb.Append($"<div class=\"section\"><div class=\"section-title\">{t.CommercialTerms}</div><table class=\"terms-table\">");
            foreach (var (label, value) in printedTerms)
                sb.Append($"<tr><td class=\"term-label\">{label}</td><td dir=\"auto\">{H(value)}</td></tr>");
            sb.Append("</table></div>");
        }

        // ---- Signatures ----
        sb.Append($"""
            <div class="section signatures">
              <div class="section-title">{t.Signatures}</div>
              <div class="sig-grid">
                <div class="sig-box"><div class="sig-role">{t.PreparedBy}</div><div class="sig-name" dir="auto">{H(m.Signatures.PreparedByName)}</div><div class="sig-position" dir="auto">{H(m.Signatures.PreparedByPosition)}</div><div class="sig-line"></div></div>
                <div class="sig-box"><div class="sig-role">{t.CommercialManager}</div><div class="sig-name" dir="auto">{H(m.Signatures.CommercialManagerName)}</div><div class="sig-position" dir="auto">{H(m.Signatures.CommercialManagerPosition)}</div><div class="sig-line"></div></div>
                <div class="sig-box"><div class="sig-role">{t.ManagingDirector}</div><div class="sig-name" dir="auto">{H(m.Signatures.ManagingDirectorName)}</div><div class="sig-position" dir="auto">{H(m.Signatures.ManagingDirectorPosition)}</div><div class="sig-line"></div></div>
                {(m.Signatures.ShowCustomerAcceptance ? $"<div class=\"sig-box\"><div class=\"sig-role\">{t.CustomerAcceptance}</div><div class=\"sig-line\"></div></div>" : "")}
              </div>
            </div>
            """);

        return sb.ToString();
    }

    private static string BuildHeaderTemplate(CustomerQuotationModel m, bool fa)
    {
        var t = Labels.For(fa);
        return $"""
            <div style="font-size:8px; width:100%; padding:0 12mm; display:flex; justify-content:space-between; direction:{(fa ? "rtl" : "ltr")}; color:{ReportTheme.SecondaryText}; font-family:Vazirmatn,Arial,sans-serif;">
              <span>{H(m.ProjectName)} — {t.QuotationNumber} {H(m.QuotationNumber)} — {t.Revision} {m.Revision}</span>
              <span>{H(fa ? m.Company.LegalNameFa : m.Company.LegalNameEn)}</span>
            </div>
            """;
    }

    private static string BuildFooterTemplate(CustomerQuotationModel m, bool fa)
    {
        var t = Labels.For(fa);
        var contact = string.Join("  |  ", new[] { m.Company.Address, m.Company.Phone, m.Company.Email, m.Company.Website }.Where(s => !string.IsNullOrWhiteSpace(s)));
        return $"""
            <div style="font-size:8px; width:100%; padding:0 12mm; direction:{(fa ? "rtl" : "ltr")}; color:{ReportTheme.SecondaryText}; font-family:Vazirmatn,Arial,sans-serif;">
              <div style="display:flex; justify-content:space-between;">
                <span>{H(contact)}</span>
                <span class="pageNumber"></span>&nbsp;{t.Of}&nbsp;<span class="totalPages"></span>
              </div>
            </div>
            """;
    }

    private static string LogoImgTag(byte[]? logoBytes) =>
        logoBytes is null ? "" : $"<img class=\"logo\" src=\"data:image/png;base64,{Convert.ToBase64String(logoBytes)}\" />";

    private static string SharedCss() => $$"""
        {{ReportAssets.FontFaceCss}}
        * { box-sizing: border-box; }
        body { font-family: 'Vazirmatn', Arial, sans-serif; color: {{ReportTheme.PrimaryText}}; font-size: 10.5pt; margin: 0; }
        .letterhead { display: flex; align-items: center; gap: 14px; border-bottom: 2px solid {{ReportTheme.PrimaryNavy}}; padding-bottom: 10px; margin-bottom: 14px; }
        .logo { height: 46px; }
        .legal-name-primary { font-size: 14pt; font-weight: 700; color: {{ReportTheme.PrimaryNavy}}; }
        .legal-name-secondary { font-size: 9pt; color: {{ReportTheme.SecondaryText}}; }
        .title-block { text-align: center; margin-bottom: 16px; }
        .doc-title { font-size: 15pt; font-weight: 700; color: {{ReportTheme.DeepBlue}}; margin-bottom: 8px; }
        .meta-table { width: 100%; border-collapse: collapse; font-size: 9.5pt; }
        .meta-table td { padding: 3px 6px; }
        .meta-table td:nth-child(odd) { color: {{ReportTheme.SecondaryText}}; white-space: nowrap; }
        .meta-table td:nth-child(even) { font-weight: 600; }
        .section { margin-bottom: 16px; break-inside: avoid; }
        .section-title { font-size: 11pt; font-weight: 700; color: {{ReportTheme.PrimaryNavy}}; border-bottom: 1px solid {{ReportTheme.Border}}; padding-bottom: 4px; margin-bottom: 8px; }
        .attn, .subject, .note { font-size: 9.5pt; margin-top: 4px; }
        .summary-cards { display: flex; gap: 10px; flex-wrap: wrap; }
        .card { flex: 1 1 21%; background: {{ReportTheme.Background}}; border: 1px solid {{ReportTheme.Border}}; border-radius: 6px; padding: 8px 10px; }
        .card-label { font-size: 8.5pt; color: {{ReportTheme.SecondaryText}}; margin-bottom: 3px; }
        .card-value { font-size: 11pt; font-weight: 700; color: {{ReportTheme.DeepBlue}}; }
        table.items-table { width: 100%; border-collapse: collapse; font-size: 9pt; }
        .items-table thead { display: table-header-group; }
        .items-table th { background: {{ReportTheme.PrimaryNavy}}; color: white; padding: 6px 5px; text-align: center; font-weight: 600; }
        .items-table td { border-bottom: 1px solid {{ReportTheme.Border}}; padding: 5px; text-align: center; }
        .items-table td.desc { text-align: start; }
        .items-table tr { break-inside: avoid; }
        .items-table td.num, .terms-table td.num { font-variant-numeric: tabular-nums; }
        .items-table tfoot td { border-top: 2px solid {{ReportTheme.PrimaryNavy}}; font-weight: 700; padding: 6px 5px; }
        .grand-total td { background: {{ReportTheme.Background}}; }
        table.terms-table { width: 100%; border-collapse: collapse; font-size: 9.5pt; }
        .terms-table td { padding: 4px 6px; vertical-align: top; border-bottom: 1px solid {{ReportTheme.Border}}; }
        .term-label { color: {{ReportTheme.SecondaryText}}; white-space: nowrap; width: 30%; font-weight: 600; }
        .signatures { margin-top: 24px; }
        .sig-grid { display: flex; gap: 12px; flex-wrap: wrap; }
        .sig-box { flex: 1 1 21%; text-align: center; }
        .sig-role { font-size: 9pt; color: {{ReportTheme.SecondaryText}}; margin-bottom: 26px; }
        .sig-name { font-weight: 700; font-size: 9.5pt; }
        .sig-position { font-size: 8.5pt; color: {{ReportTheme.SecondaryText}}; }
        .sig-line { border-top: 1px solid {{ReportTheme.PrimaryText}}; margin-top: 6px; }
        """;

    private static string? H(string? s) => s is null ? null : WebUtility.HtmlEncode(s);
    private static string FormatIrr(decimal value) => value.ToString("N0", CultureInfo.InvariantCulture);
    private static string FormatForeign(decimal value) => value.ToString("N2", CultureInfo.InvariantCulture);
}
