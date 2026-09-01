using Microsoft.Playwright;

namespace NTNP.Pricing.Reporting.Rendering;

public sealed record PdfRenderOptions(
    string? HeaderHtml = null,
    string? FooterHtml = null,
    string TopMargin = "22mm",
    string BottomMargin = "20mm",
    string LeftMargin = "12mm",
    string RightMargin = "12mm",
    bool Landscape = false);

/// <summary>
/// Renders a self-contained HTML document (all CSS/fonts/images inlined — no external resource
/// loads, so this works with no internet access, matching Section 1's offline requirement) to PDF
/// via headless Chromium. One <see cref="IBrowser"/> instance is kept alive for the process
/// lifetime; each render opens and closes its own page. See ASSUMPTIONS.md for why Chromium was
/// chosen over MigraDoc/PdfSharp (Persian RTL/BiDi correctness).
/// </summary>
public sealed class HtmlToPdfRenderer : IAsyncDisposable
{
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public async Task<byte[]> RenderPdfAsync(string html, PdfRenderOptions options, CancellationToken ct = default)
    {
        var browser = await GetBrowserAsync();
        await using var page = await browser.NewPageAsync();
        await page.SetContentAsync(html, new PageSetContentOptions { WaitUntil = WaitUntilState.NetworkIdle });

        var pdfOptions = new PagePdfOptions
        {
            Format = "A4",
            Landscape = options.Landscape,
            PrintBackground = true,
            DisplayHeaderFooter = options.HeaderHtml is not null || options.FooterHtml is not null,
            HeaderTemplate = options.HeaderHtml ?? "<span></span>",
            FooterTemplate = options.FooterHtml ?? "<span></span>",
            Margin = new Margin
            {
                Top = options.TopMargin,
                Bottom = options.BottomMargin,
                Left = options.LeftMargin,
                Right = options.RightMargin,
            },
        };

        return await page.PdfAsync(pdfOptions);
    }

    private async Task<IBrowser> GetBrowserAsync()
    {
        if (_browser is not null) return _browser;

        await _initLock.WaitAsync();
        try
        {
            if (_browser is not null) return _browser;

            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Args = new[] { "--no-sandbox" }, // required when the API runs under a locked-down Windows Service account
                ExecutablePath = ResolveExecutablePathOverride(),
            });
            return _browser;
        }
        finally
        {
            _initLock.Release();
        }
    }

    /// <summary>
    /// On a normal Windows Server deployment (after `playwright install chromium`), Playwright
    /// resolves its own browser path and this returns null. In this repository's Linux CI/dev
    /// sandbox the pre-installed Chromium's revision number doesn't match what the pinned
    /// Microsoft.Playwright NuGet version expects, so a `PLAYWRIGHT_BROWSERS_PATH/chromium`
    /// executable (present in that sandbox image) is used directly if found.
    /// </summary>
    private static string? ResolveExecutablePathOverride()
    {
        var browsersPath = Environment.GetEnvironmentVariable("PLAYWRIGHT_BROWSERS_PATH");
        if (string.IsNullOrEmpty(browsersPath)) return null;

        var candidate = Path.Combine(browsersPath, "chromium");
        return File.Exists(candidate) || Directory.Exists(candidate) ? candidate : null;
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null) await _browser.CloseAsync();
        _playwright?.Dispose();
    }
}
