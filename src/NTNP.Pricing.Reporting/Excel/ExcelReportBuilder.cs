using ClosedXML.Excel;
using System.Linq;
using NTNP.Pricing.Reporting.Models;

namespace NTNP.Pricing.Reporting.Excel;

/// <summary>Section 27 — XLSX exports for internal reports, via ClosedXML (MIT).</summary>
public static class ExcelReportBuilder
{
    public static byte[] BuildBomMtoWorkbook(BomMtoReportModel model)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(SanitizeSheetName(model.Title));

        WriteTitleBlock(sheet, model.Title, model.ProjectCode, model.ProjectName, model.RevisionNumber, model.GeneratedAtUtc);

        var headerRow = 5;
        string[] headers = { "Row", "Code", "Part Number", "Description", "Brand", "Model", "Unit",
            "Total Required Quantity", "Purchase Currency", "Snapshot Unit Cost (IRR)", "Total Procurement Cost (IRR)", "Related Panel Types", "Notes" };
        for (var i = 0; i < headers.Length; i++) sheet.Cell(headerRow, i + 1).Value = headers[i];
        StyleHeaderRow(sheet.Range(headerRow, 1, headerRow, headers.Length));

        var row = headerRow + 1;
        foreach (var r in model.Rows)
        {
            sheet.Cell(row, 1).Value = r.Row;
            sheet.Cell(row, 2).Value = r.Code;
            sheet.Cell(row, 3).Value = r.PartNumber ?? "";
            sheet.Cell(row, 4).Value = r.Description;
            sheet.Cell(row, 5).Value = r.Brand ?? "";
            sheet.Cell(row, 6).Value = r.Model ?? "";
            sheet.Cell(row, 7).Value = r.Unit;
            sheet.Cell(row, 8).Value = r.Quantity;
            sheet.Cell(row, 9).Value = r.PurchaseCurrencyCode ?? "";
            sheet.Cell(row, 10).Value = r.SnapshotUnitCostIrr;
            sheet.Cell(row, 11).Value = r.TotalCostIrr;
            sheet.Cell(row, 12).Value = string.Join(", ", r.RelatedPanelTypes);
            sheet.Cell(row, 13).Value = r.Notes ?? "";
            row++;
        }

        sheet.Range(headerRow + 1, 10, row - 1, 11).Style.NumberFormat.Format = "#,##0";
        sheet.Cell(row, 4).Value = "Grand Total";
        sheet.Cell(row, 11).Value = model.GrandTotalIrr;
        sheet.Range(row, 1, row, 13).Style.Font.Bold = true;

        sheet.Columns().AdjustToContents();
        sheet.RightToLeft = true;
        return ToBytes(workbook);
    }

    public static byte[] BuildInternalCostingWorkbook(InternalCostingReportModel model)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Internal Costing Report");

        WriteTitleBlock(sheet, "Internal Costing Report", model.ProjectCode, model.ProjectName, model.RevisionNumber, model.GeneratedAtUtc);

        var headerRow = 5;
        string[] headers = { "Row", "Cell Code", "Panel Type", "Description", "Qty", "Equipment Cost/Panel", "BODY+ES Cost/Panel",
            "Other Direct Cost/Panel", "Total Cost/Panel", "Total Line Cost", "Pricing Method", "Rate", "Selling Price/Panel",
            "Total Line Selling (IRR)", "Rial Share", "Rial Payable", "Currency", "Foreign Share", "Selling Rate", "Foreign Payable",
            "Profit", "Gross Margin", "Reconciliation", "Override" };
        for (var i = 0; i < headers.Length; i++) sheet.Cell(headerRow, i + 1).Value = headers[i];
        StyleHeaderRow(sheet.Range(headerRow, 1, headerRow, headers.Length));

        var row = headerRow + 1;
        foreach (var l in model.Lines)
        {
            sheet.Cell(row, 1).Value = l.Row;
            sheet.Cell(row, 2).Value = l.CellCode;
            sheet.Cell(row, 3).Value = l.PanelType;
            sheet.Cell(row, 4).Value = l.Description;
            sheet.Cell(row, 5).Value = l.Quantity;
            sheet.Cell(row, 6).Value = l.EquipmentCostPerPanel;
            sheet.Cell(row, 7).Value = l.BodyEsCostPerPanel;
            sheet.Cell(row, 8).Value = l.OtherDirectCostPerPanel;
            sheet.Cell(row, 9).Value = l.TotalCostPerPanel;
            sheet.Cell(row, 10).Value = l.TotalLineCost;
            sheet.Cell(row, 11).Value = l.PricingMethod;
            sheet.Cell(row, 12).Value = l.PricingRate;
            sheet.Cell(row, 13).Value = l.SellingPricePerPanel;
            sheet.Cell(row, 14).Value = l.TotalLineSellingPriceIrr;
            sheet.Cell(row, 15).Value = l.RialShare;
            sheet.Cell(row, 16).Value = l.RialPayable;
            sheet.Cell(row, 17).Value = l.QuotationCurrency;
            sheet.Cell(row, 18).Value = l.ForeignShare;
            sheet.Cell(row, 19).Value = l.SellingExchangeRate;
            sheet.Cell(row, 20).Value = l.ForeignPayable;
            sheet.Cell(row, 21).Value = l.Profit;
            sheet.Cell(row, 22).Value = l.GrossMargin;
            sheet.Cell(row, 23).Value = l.ReconciliationPassed ? "PASS" : "FAIL";
            sheet.Cell(row, 23).Style.Font.FontColor = l.ReconciliationPassed ? XLColor.FromHtml("#14865C") : XLColor.FromHtml("#C43D3D");
            sheet.Cell(row, 24).Value = l.HasOverride ? "Yes" : "";
            row++;
        }

        sheet.Range(headerRow + 1, 6, row - 1, 10).Style.NumberFormat.Format = "#,##0";
        sheet.Range(headerRow + 1, 13, row - 1, 14).Style.NumberFormat.Format = "#,##0";
        sheet.Range(headerRow + 1, 16, row - 1, 16).Style.NumberFormat.Format = "#,##0";
        sheet.Range(headerRow + 1, 12, row - 1, 12).Style.NumberFormat.Format = "0.0%";
        sheet.Range(headerRow + 1, 15, row - 1, 15).Style.NumberFormat.Format = "0%";
        sheet.Range(headerRow + 1, 18, row - 1, 18).Style.NumberFormat.Format = "0%";
        sheet.Range(headerRow + 1, 21, row - 1, 21).Style.NumberFormat.Format = "#,##0";
        sheet.Range(headerRow + 1, 22, row - 1, 22).Style.NumberFormat.Format = "0.00%";

        var summaryRow = row + 1;
        var t = model.Totals;
        WriteSummaryRow(sheet, summaryRow, "Total Project Cost (IRR)", t.TotalProjectCostIrr);
        WriteSummaryRow(sheet, summaryRow + 1, "Total Project Selling Price (IRR)", t.TotalProjectSellingPriceIrr);
        WriteSummaryRow(sheet, summaryRow + 2, "Project Profit (IRR)", t.ProjectProfitIrr);
        sheet.Cell(summaryRow + 3, 1).Value = "Project Gross Margin";
        sheet.Cell(summaryRow + 3, 2).Value = t.ProjectGrossMargin;
        sheet.Cell(summaryRow + 3, 2).Style.NumberFormat.Format = "0.00%";
        sheet.Cell(summaryRow + 4, 1).Value = "Reconciliation";
        sheet.Cell(summaryRow + 4, 2).Value = t.ReconciliationPassed ? "PASS" : "FAIL";

        sheet.Columns().AdjustToContents();
        sheet.RightToLeft = true;
        return ToBytes(workbook);
    }

    public static byte[] BuildRevisionComparisonWorkbook(RevisionComparisonReportModel model)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Revision Comparison");

        sheet.Cell(1, 1).Value = $"Revision Comparison — {model.ProjectCode} — {model.ProjectName}";
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(2, 1).Value = $"From Rev {model.FromRevision} to Rev {model.ToRevision}";

        sheet.Cell(4, 1).Value = "Cost Delta (IRR)"; sheet.Cell(4, 2).Value = model.CostDeltaIrr;
        sheet.Cell(5, 1).Value = "Selling Price Delta (IRR)"; sheet.Cell(5, 2).Value = model.SellingPriceDeltaIrr;
        sheet.Cell(6, 1).Value = "Profit Delta (IRR)"; sheet.Cell(6, 2).Value = model.ProfitDeltaIrr;
        sheet.Cell(7, 1).Value = "Gross Margin Delta"; sheet.Cell(7, 2).Value = model.GrossMarginDelta;
        sheet.Cell(7, 2).Style.NumberFormat.Format = "0.00%";

        var headerRow = 9;
        string[] headers = { "Cell Code", "Field", "Old Value", "New Value" };
        for (var i = 0; i < headers.Length; i++) sheet.Cell(headerRow, i + 1).Value = headers[i];
        StyleHeaderRow(sheet.Range(headerRow, 1, headerRow, headers.Length));

        var row = headerRow + 1;
        foreach (var c in model.ChangedFields)
        {
            sheet.Cell(row, 1).Value = c.CellCode;
            sheet.Cell(row, 2).Value = c.FieldName;
            sheet.Cell(row, 3).Value = c.OldValue;
            sheet.Cell(row, 4).Value = c.NewValue;
            row++;
        }

        sheet.Columns().AdjustToContents();
        return ToBytes(workbook);
    }

    private static void WriteTitleBlock(IXLWorksheet sheet, string title, string projectCode, string projectName, int revision, DateTimeOffset generatedAtUtc)
    {
        sheet.Cell(1, 1).Value = title;
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 1).Style.Font.FontSize = 14;
        sheet.Cell(2, 1).Value = $"Project: {projectCode} — {projectName}";
        sheet.Cell(3, 1).Value = $"Revision: {revision}   Generated: {generatedAtUtc:yyyy-MM-dd HH:mm} UTC";
    }

    private static void WriteSummaryRow(IXLWorksheet sheet, int row, string label, decimal value)
    {
        sheet.Cell(row, 1).Value = label;
        sheet.Cell(row, 2).Value = value;
        sheet.Cell(row, 2).Style.NumberFormat.Format = "#,##0";
        sheet.Range(row, 1, row, 2).Style.Font.Bold = true;
    }

    private static void StyleHeaderRow(IXLRange range)
    {
        range.Style.Font.Bold = true;
        range.Style.Font.FontColor = XLColor.White;
        range.Style.Fill.BackgroundColor = XLColor.FromHtml("#0B1F3A");
        range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
    }

    private static byte[] ToBytes(XLWorkbook workbook)
    {
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max];

    /// <summary>
    /// Excel worksheet names reject <c>\ / ? * [ ] :</c>, cannot exceed 31 characters, and cannot be
    /// empty or start/end with an apostrophe — <see cref="BomMtoReportModel.Title"/> is free text
    /// (e.g. "Electrical BOM / Material Take-Off") supplied by the Api layer, not pre-validated
    /// against those rules, so it is sanitized here rather than trusted verbatim.
    /// </summary>
    private static string SanitizeSheetName(string title)
    {
        var cleaned = new string(title.Select(c => "\\/?*[]:".Contains(c) ? '-' : c).ToArray()).Trim();
        cleaned = Truncate(cleaned, 31).Trim('\'');
        return string.IsNullOrWhiteSpace(cleaned) ? "Report" : cleaned;
    }
}
