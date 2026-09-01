using System.Globalization;
using System.Net;
using System.Text;
using NTNP.Pricing.Reporting.Assets;
using NTNP.Pricing.Reporting.Models;

namespace NTNP.Pricing.Reporting.Rendering;

/// <summary>
/// Section 27 — internal reports (Internal Costing Report, per-panel/project BOM, Electrical/BODY+ES/
/// Combined MTO, Revision Comparison). Every page is stamped "INTERNAL – CONFIDENTIAL" /
/// "داخلی – محرمانه" top and bottom. Entirely separate template code from
/// <see cref="CustomerQuotationHtmlBuilder"/> — this class is the only place in the system allowed
/// to render cost/margin/profit/rate/override data onto a PDF page.
/// </summary>
public static class InternalReportHtmlBuilder
{
    public static (string Body, string Header, string Footer) BuildCostingReport(InternalCostingReportModel m)
    {
        var sb = new StringBuilder();
        sb.Append(Banner(m.Company));
        sb.Append($"""
            <div class="title-block">
              <div class="doc-title">گزارش داخلی بهای تمام‌شده — Internal Costing Report</div>
              <table class="meta-table">
                <tr><td>پروژه</td><td dir="auto">{H(m.ProjectCode)} — {H(m.ProjectName)}</td><td>مشتری</td><td dir="auto">{H(m.CustomerName)}</td></tr>
                <tr><td>بازنگری</td><td>{m.RevisionNumber} ({H(m.RevisionStatus)})</td><td>تاریخ تولید</td><td>{m.GeneratedAtUtc:yyyy-MM-dd HH:mm} UTC</td></tr>
              </table>
            </div>
            """);

        sb.Append("""
            <table class="items-table"><thead><tr>
            <th>ردیف</th><th>کد سلول</th><th>تیپ</th><th>شرح</th><th>تعداد</th>
            <th>هزینه تجهیزات/تابلو</th><th>هزینه بدنه/تابلو</th><th>سایر مستقیم</th><th>جمع هزینه/تابلو</th>
            <th>جمع هزینه خط</th><th>روش قیمت‌گذاری</th><th>نرخ</th><th>فروش/تابلو</th><th>جمع فروش (ریال)</th>
            <th>سهم ریالی</th><th>پرداختنی ریالی</th><th>ارز</th><th>سهم ارزی</th><th>نرخ فروش</th><th>پرداختنی ارزی</th>
            <th>سود</th><th>حاشیه سود</th><th>تطبیق</th><th>اصلاحیه</th>
            </tr></thead><tbody>
            """);
        foreach (var l in m.Lines)
        {
            sb.Append($"""
                <tr>
                  <td>{l.Row}</td><td>{H(l.CellCode)}</td><td>{H(l.PanelType)}</td><td class="desc" dir="auto">{H(l.Description)}</td>
                  <td class="num">{l.Quantity:0.##}</td><td class="num">{N0(l.EquipmentCostPerPanel)}</td>
                  <td class="num">{N0(l.BodyEsCostPerPanel)}</td><td class="num">{N0(l.OtherDirectCostPerPanel)}</td>
                  <td class="num">{N0(l.TotalCostPerPanel)}</td><td class="num">{N0(l.TotalLineCost)}</td>
                  <td>{H(l.PricingMethod)}</td><td class="num">{l.PricingRate:P1}</td><td class="num">{N0(l.SellingPricePerPanel)}</td>
                  <td class="num">{N0(l.TotalLineSellingPriceIrr)}</td><td class="num">{l.RialShare:P0}</td>
                  <td class="num">{N0(l.RialPayable)}</td><td>{H(l.QuotationCurrency)}</td><td class="num">{l.ForeignShare:P0}</td>
                  <td class="num">{N2(l.SellingExchangeRate)}</td><td class="num">{N2(l.ForeignPayable)}</td>
                  <td class="num">{N0(l.Profit)}</td><td class="num">{l.GrossMargin:P2}</td>
                  <td class="{(l.ReconciliationPassed ? "pass" : "fail")}">{(l.ReconciliationPassed ? "PASS" : "FAIL")}</td>
                  <td>{(l.HasOverride ? "دارد" : "-")}</td>
                </tr>
                """);
        }
        sb.Append("</tbody></table>");

        var t = m.Totals;
        sb.Append($"""
            <div class="section">
              <div class="section-title">خلاصه پروژه — Project Summary</div>
              <div class="summary-cards">
                <div class="card"><div class="card-label">بهای تمام‌شده پروژه</div><div class="card-value">{N0(t.TotalProjectCostIrr)}</div></div>
                <div class="card"><div class="card-label">قیمت فروش کل</div><div class="card-value">{N0(t.TotalProjectSellingPriceIrr)}</div></div>
                <div class="card"><div class="card-label">سود پروژه</div><div class="card-value">{N0(t.ProjectProfitIrr)}</div></div>
                <div class="card"><div class="card-label">حاشیه سود</div><div class="card-value">{t.ProjectGrossMargin:P2}</div></div>
                <div class="card"><div class="card-label">مبلغ ریالی</div><div class="card-value">{N0(t.TotalRialPayable)}</div></div>
                <div class="card"><div class="card-label">مبلغ ارزی</div><div class="card-value">{N2(t.TotalForeignPayable)}</div></div>
                <div class="card"><div class="card-label">وضعیت تطبیق</div><div class="card-value {(t.ReconciliationPassed ? "pass" : "fail")}">{(t.ReconciliationPassed ? "PASS" : "FAIL")}</div></div>
              </div>
              {(t.ApprovalBlockers.Count == 0 ? "" : $"<div class='blockers'><b>موانع تأیید:</b><ul>{string.Concat(t.ApprovalBlockers.Select(b => $"<li>{H(b)}</li>"))}</ul></div>")}
            </div>
            """);

        return WrapDocument(sb.ToString(), m.Company);
    }

    public static (string Body, string Header, string Footer) BuildBomMtoReport(BomMtoReportModel m)
    {
        var sb = new StringBuilder();
        sb.Append(Banner(m.Company));
        sb.Append($"""
            <div class="title-block">
              <div class="doc-title">{H(m.Title)}</div>
              <table class="meta-table">
                <tr><td>پروژه</td><td dir="auto">{H(m.ProjectCode)} — {H(m.ProjectName)}</td><td>بازنگری</td><td>{m.RevisionNumber}</td></tr>
                <tr><td>تاریخ تولید</td><td colspan="3">{m.GeneratedAtUtc:yyyy-MM-dd HH:mm} UTC</td></tr>
              </table>
            </div>
            """);

        sb.Append("""
            <table class="items-table"><thead><tr>
            <th>ردیف</th><th>کد</th><th>پارت نامبر</th><th>شرح</th><th>برند</th><th>مدل</th><th>واحد</th>
            <th>تعداد کل موردنیاز</th><th>ارز خرید</th><th>هزینه واحد (ریال)</th><th>هزینه کل (ریال)</th><th>تیپ‌های مرتبط</th>
            </tr></thead><tbody>
            """);
        foreach (var r in m.Rows)
        {
            sb.Append($"""
                <tr>
                  <td>{r.Row}</td><td>{H(r.Code)}</td><td>{H(r.PartNumber) ?? "-"}</td><td class="desc" dir="auto">{H(r.Description)}</td>
                  <td>{H(r.Brand) ?? "-"}</td><td>{H(r.Model) ?? "-"}</td><td>{H(r.Unit)}</td>
                  <td class="num">{r.Quantity:0.##}</td><td>{H(r.PurchaseCurrencyCode) ?? "-"}</td>
                  <td class="num">{N0(r.SnapshotUnitCostIrr)}</td><td class="num">{N0(r.TotalCostIrr)}</td>
                  <td>{H(string.Join(", ", r.RelatedPanelTypes))}</td>
                </tr>
                """);
        }
        sb.Append($"""
            </tbody><tfoot><tr class="grand-total"><td colspan="10">جمع کل — Grand Total</td><td class="num" colspan="2">{N0(m.GrandTotalIrr)} ریال</td></tr></tfoot>
            </table>
            """);

        return WrapDocument(sb.ToString(), m.Company);
    }

    public static (string Body, string Header, string Footer) BuildRevisionComparison(RevisionComparisonReportModel m)
    {
        var sb = new StringBuilder();
        sb.Append(Banner(m.Company));
        sb.Append($"""
            <div class="title-block">
              <div class="doc-title">مقایسه بازنگری — Revision Comparison</div>
              <table class="meta-table">
                <tr><td>پروژه</td><td dir="auto">{H(m.ProjectCode)} — {H(m.ProjectName)}</td><td>از بازنگری</td><td>{m.FromRevision} → {m.ToRevision}</td></tr>
              </table>
            </div>
            <div class="summary-cards">
              <div class="card"><div class="card-label">تغییر بهای تمام‌شده</div><div class="card-value">{N0(m.CostDeltaIrr)}</div></div>
              <div class="card"><div class="card-label">تغییر قیمت فروش</div><div class="card-value">{N0(m.SellingPriceDeltaIrr)}</div></div>
              <div class="card"><div class="card-label">تغییر سود</div><div class="card-value">{N0(m.ProfitDeltaIrr)}</div></div>
              <div class="card"><div class="card-label">تغییر حاشیه سود</div><div class="card-value">{m.GrossMarginDelta:P2}</div></div>
            </div>
            <table class="items-table"><thead><tr><th>کد سلول</th><th>فیلد</th><th>مقدار قبلی</th><th>مقدار جدید</th></tr></thead><tbody>
            """);
        foreach (var c in m.ChangedFields)
            sb.Append($"<tr><td>{H(c.CellCode)}</td><td>{H(c.FieldName)}</td><td>{H(c.OldValue)}</td><td>{H(c.NewValue)}</td></tr>");
        sb.Append("</tbody></table>");

        return WrapDocument(sb.ToString(), m.Company);
    }

    private static string Banner(CompanyBranding company) => $"""
        <div class="confidential-banner">{H(company.ConfidentialityLabelFa)} — {H(company.ConfidentialityLabelEn)}</div>
        """;

    private static (string, string, string) WrapDocument(string body, CompanyBranding company)
    {
        var doc = $$"""
            <!DOCTYPE html>
            <html dir="rtl" lang="fa">
            <head><meta charset="utf-8" /><style>{{SharedCss()}}</style></head>
            <body>{{body}}<div class="confidential-banner bottom">{{H(company.ConfidentialityLabelFa)}} — {{H(company.ConfidentialityLabelEn)}}</div></body>
            </html>
            """;

        var header = $"""
            <div style="font-size:8px; width:100%; padding:0 12mm; direction:rtl; color:{ReportTheme.Error}; font-weight:700; text-align:center; font-family:Vazirmatn,Arial,sans-serif;">
              {H(company.ConfidentialityLabelFa)} — {H(company.ConfidentialityLabelEn)}
            </div>
            """;
        var footer = """
            <div style="font-size:8px; width:100%; padding:0 12mm; direction:rtl; display:flex; justify-content:space-between; font-family:Vazirmatn,Arial,sans-serif;">
              <span>سند داخلی — توزیع خارج از شرکت ممنوع</span>
              <span><span class="pageNumber"></span>&nbsp;از&nbsp;<span class="totalPages"></span></span>
            </div>
            """;
        return (doc, header, footer);
    }

    private static string SharedCss() => $$"""
        {{ReportAssets.FontFaceCss}}
        * { box-sizing: border-box; }
        body { font-family: 'Vazirmatn', Arial, sans-serif; color: {{ReportTheme.PrimaryText}}; font-size: 8.5pt; margin: 0; }
        .confidential-banner { background: {{ReportTheme.Error}}; color: white; text-align: center; font-weight: 700; padding: 4px; font-size: 9pt; margin-bottom: 10px; }
        .confidential-banner.bottom { margin-top: 14px; margin-bottom: 0; }
        .title-block { margin-bottom: 12px; }
        .doc-title { font-size: 13pt; font-weight: 700; color: {{ReportTheme.DeepBlue}}; margin-bottom: 6px; }
        .meta-table { width: 100%; border-collapse: collapse; font-size: 9pt; }
        .meta-table td { padding: 2px 6px; }
        .meta-table td:nth-child(odd) { color: {{ReportTheme.SecondaryText}}; white-space: nowrap; }
        .meta-table td:nth-child(even) { font-weight: 600; }
        table.items-table { width: 100%; border-collapse: collapse; font-size: 6.8pt; margin-top: 8px; table-layout: fixed; }
        .items-table thead { display: table-header-group; }
        .items-table th { background: {{ReportTheme.PrimaryNavy}}; color: white; padding: 3px 2px; text-align: center; font-weight: 600; word-break: break-word; }
        .items-table td { border-bottom: 1px solid {{ReportTheme.Border}}; padding: 2px; text-align: center; overflow-wrap: break-word; }
        .items-table td.desc { text-align: start; }
        .items-table tr { break-inside: avoid; }
        .items-table td.num { font-variant-numeric: tabular-nums; }
        .items-table tfoot td { border-top: 2px solid {{ReportTheme.PrimaryNavy}}; font-weight: 700; padding: 5px 3px; }
        .grand-total td { background: {{ReportTheme.Background}}; }
        .section { margin-top: 14px; break-inside: avoid; }
        .section-title { font-size: 10pt; font-weight: 700; color: {{ReportTheme.PrimaryNavy}}; border-bottom: 1px solid {{ReportTheme.Border}}; padding-bottom: 3px; margin-bottom: 6px; }
        .summary-cards { display: flex; gap: 8px; flex-wrap: wrap; margin-top: 8px; }
        .card { flex: 1 1 21%; background: {{ReportTheme.Background}}; border: 1px solid {{ReportTheme.Border}}; border-radius: 6px; padding: 6px 8px; }
        .card-label { font-size: 7.5pt; color: {{ReportTheme.SecondaryText}}; margin-bottom: 2px; }
        .card-value { font-size: 10pt; font-weight: 700; color: {{ReportTheme.DeepBlue}}; }
        .card-value.pass, .pass { color: {{ReportTheme.Success}}; }
        .card-value.fail, .fail { color: {{ReportTheme.Error}}; font-weight: 700; }
        .blockers { margin-top: 8px; font-size: 8.5pt; color: {{ReportTheme.Error}}; }
        """;

    private static string? H(string? s) => s is null ? null : WebUtility.HtmlEncode(s);
    private static string N0(decimal v) => v.ToString("N0", CultureInfo.InvariantCulture);
    private static string N2(decimal v) => v.ToString("N2", CultureInfo.InvariantCulture);
}
