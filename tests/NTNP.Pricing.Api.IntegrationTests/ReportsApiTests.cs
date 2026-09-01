using System.Net;
using System.Net.Http.Json;
using NTNP.Pricing.Contracts.Common;
using NTNP.Pricing.Contracts.Files;
using NTNP.Pricing.Contracts.Projects;
using UglyToad.PdfPig;

namespace NTNP.Pricing.Api.IntegrationTests;

/// <summary>
/// Sections 16/19/26/32 — report generation exercised through the real HTTP pipeline: real
/// Playwright/Chromium rendering (same DI-registered <c>IReportRenderer</c> the Api process uses in
/// production), real file persistence via <c>IFileStorageService</c>, and the Section 29 customer/
/// internal field-separation rule re-verified at this outer layer (not just inside Reporting.Tests).
/// </summary>
public sealed class ReportsApiTests : IntegrationTestBase
{
    public ReportsApiTests(ApiTestFactory factory) : base(factory)
    {
    }

    private async Task<(Guid RevisionId, HttpClient Client)> LoadSeededRevisionAsync()
    {
        var client = await CreateAdminClientAsync();
        var page = await (await client.GetAsync("/api/projects?search=PRJ-0001")).Content.ReadFromJsonAsync<PagedResult<ProjectListItemDto>>(Json);
        var listItem = Assert.Single(page!.Items, p => p.ProjectCode == "PRJ-0001");
        var project = (await (await client.GetAsync($"/api/projects/{listItem.Id}")).Content.ReadFromJsonAsync<ProjectDto>(Json))!;
        return (project.CurrentRevisionId!.Value, client);
    }

    [Fact]
    public async Task Customer_Quotation_Pdf_Downloads_Successfully_And_Never_Leaks_Internal_Cost_Figures()
    {
        var (revisionId, client) = await LoadSeededRevisionAsync();

        // English-only here so the numeric assertions below are extracted reliably by PdfPig — the
        // bilingual/RTL pagination path itself is already covered by Reporting.Tests.PdfQualityTests.
        var response = await client.GetAsync($"/api/project-revisions/{revisionId}/reports/quotation?language=en");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 1000, "expected a real, non-empty PDF");

        // Section 29 — the internal-only cost figures (3,030,000,000 total cost; 909,000,000
        // profit) must never appear in the customer-facing document, even though they exist on the
        // very same revision's internal costing report. Extracted via PdfPig's real text layer (a
        // raw byte search would be meaningless — Chromium's PDF content streams are compressed).
        using var doc = PdfDocument.Open(bytes);
        var pdfText = string.Join(" ", doc.GetPages().Select(p => p.Text));
        Assert.DoesNotContain("3,030,000,000", pdfText);
        Assert.DoesNotContain("909,000,000", pdfText);
        Assert.Contains("3,939,000,000", pdfText); // the customer-facing selling price IS expected
    }

    [Fact]
    public async Task Internal_Costing_Report_Excel_Contains_The_Cost_And_Margin_Figures_And_Is_Registered_As_A_StoredFile()
    {
        var (revisionId, client) = await LoadSeededRevisionAsync();

        var response = await client.GetAsync($"/api/project-revisions/{revisionId}/reports/internal-costing?format=xlsx");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", response.Content.Headers.ContentType?.MediaType);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 500);

        // Section 32 — every generated report is registered as a durable StoredFile the client can list/re-download.
        var filesResponse = await client.GetAsync($"/api/files?projectRevisionId={revisionId}&category=InternalReport");
        Assert.Equal(HttpStatusCode.OK, filesResponse.StatusCode);
        var files = await filesResponse.Content.ReadFromJsonAsync<IReadOnlyList<StoredFileDto>>(Json);
        Assert.NotEmpty(files!);

        var downloadResponse = await client.GetAsync($"/api/files/{files![0].Id}/download");
        Assert.Equal(HttpStatusCode.OK, downloadResponse.StatusCode);
        Assert.Equal(bytes.LongLength > 0, (await downloadResponse.Content.ReadAsByteArrayAsync()).LongLength > 0);
    }

    [Fact]
    public async Task Mto_Report_Pdf_And_Excel_Both_Generate_Successfully_For_All_Three_Kinds()
    {
        var (revisionId, client) = await LoadSeededRevisionAsync();

        foreach (var kind in new[] { "electrical", "bodyes", "combined" })
        {
            foreach (var format in new[] { "pdf", "xlsx" })
            {
                var response = await client.GetAsync($"/api/project-revisions/{revisionId}/reports/mto?kind={kind}&format={format}");
                Assert.True(response.IsSuccessStatusCode, $"kind={kind} format={format} failed with {response.StatusCode}");
            }
        }
    }

    [Fact]
    public async Task Viewer_Role_Can_Still_Download_Reports_ViewOnly_Covers_All_Five_Roles()
    {
        var (revisionId, adminClient) = await LoadSeededRevisionAsync();

        var email = $"viewer-reports-{Guid.NewGuid():N}@ntnp.local";
        await adminClient.PostAsJsonAsync("/api/users",
            new NTNP.Pricing.Contracts.Auth.CreateUserRequest(email, email, "Viewer Reports Test", "Ntnp!Viewer123", new[] { NTNP.Pricing.Domain.Enums.Roles.Viewer }), Json);
        var viewerClient = await CreateAuthenticatedClientAsync(email, "Ntnp!Viewer123");

        var response = await viewerClient.GetAsync($"/api/project-revisions/{revisionId}/reports/quotation?language=en");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
