using System.Reflection;

namespace NTNP.Pricing.Reporting.Assets;

/// <summary>Loads embedded fonts (real Vazirmatn, OFL-1.1) as base64 for inlining into report HTML/CSS.</summary>
public static class ReportAssets
{
    private static readonly Lazy<string> VazirmatnRegularBase64 = new(() => LoadBase64("Vazirmatn-Regular.ttf"));
    private static readonly Lazy<string> VazirmatnMediumBase64 = new(() => LoadBase64("Vazirmatn-Medium.ttf"));
    private static readonly Lazy<string> VazirmatnBoldBase64 = new(() => LoadBase64("Vazirmatn-Bold.ttf"));

    public static string VazirmatnRegular => VazirmatnRegularBase64.Value;
    public static string VazirmatnMedium => VazirmatnMediumBase64.Value;
    public static string VazirmatnBold => VazirmatnBoldBase64.Value;

    private static string LoadBase64(string fontFileName)
    {
        var assembly = typeof(ReportAssets).Assembly;
        var resourceName = assembly.GetManifestResourceNames().Single(n => n.EndsWith(fontFileName, StringComparison.OrdinalIgnoreCase));
        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        return Convert.ToBase64String(memoryStream.ToArray());
    }

    /// <summary>The shared @font-face + base typography CSS block used by every report template.</summary>
    public static string FontFaceCss => $$"""
        @font-face { font-family: 'Vazirmatn'; src: url(data:font/ttf;base64,{{VazirmatnRegular}}) format('truetype'); font-weight: 400; }
        @font-face { font-family: 'Vazirmatn'; src: url(data:font/ttf;base64,{{VazirmatnMedium}}) format('truetype'); font-weight: 500; }
        @font-face { font-family: 'Vazirmatn'; src: url(data:font/ttf;base64,{{VazirmatnBold}}) format('truetype'); font-weight: 700; }
        """;
}
