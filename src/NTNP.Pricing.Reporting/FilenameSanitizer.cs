using System.Text;

namespace NTNP.Pricing.Reporting;

/// <summary>Section 26 — builds the mandated quotation filename pattern and strips invalid Windows filename characters.</summary>
public static class FilenameSanitizer
{
    private static readonly char[] InvalidChars = Path.GetInvalidFileNameChars().Concat(new[] { ':', '*', '?', '"', '<', '>', '|' }).Distinct().ToArray();

    public static string Sanitize(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var c in value)
            builder.Append(InvalidChars.Contains(c) ? '-' : c);
        return builder.ToString().Trim();
    }

    /// <summary>Pattern: NTNP-Quotation-{QuotationNumber}-Rev-{Revision}-{Customer}-{Project}.pdf</summary>
    public static string BuildQuotationFileName(string quotationNumber, int revision, string customerName, string projectName) =>
        Sanitize($"NTNP-Quotation-{quotationNumber}-Rev-{revision}-{customerName}-{projectName}.pdf");
}
