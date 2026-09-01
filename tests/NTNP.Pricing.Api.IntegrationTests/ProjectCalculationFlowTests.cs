using System.Net;
using System.Net.Http.Json;
using NTNP.Pricing.Contracts.Common;
using NTNP.Pricing.Contracts.Mto;
using NTNP.Pricing.Contracts.Projects;

namespace NTNP.Pricing.Api.IntegrationTests;

/// <summary>
/// Section 20 — the mandatory reference calculation scenario, verified end-to-end through the real
/// HTTP/JSON/EF pipeline against the seeded sample project PRJ-0001 (see SampleProjectSeeder), not
/// just in-process against the Domain layer (that is <see cref="Domain.Tests.ReferenceCalculationScenarioTests"/>).
/// Also exercises the Section 16 MTO generator and the Section 21 approval workflow end to end.
/// </summary>
public sealed class ProjectCalculationFlowTests : IntegrationTestBase
{
    public ProjectCalculationFlowTests(ApiTestFactory factory) : base(factory)
    {
    }

    private async Task<(ProjectDto Project, ProjectRevisionDto Revision, HttpClient Client)> LoadSeededSampleProjectAsync()
    {
        var client = await CreateAdminClientAsync();

        var searchResponse = await client.GetAsync("/api/projects?search=PRJ-0001");
        Assert.Equal(HttpStatusCode.OK, searchResponse.StatusCode);
        var page = await searchResponse.Content.ReadFromJsonAsync<PagedResult<ProjectListItemDto>>(Json);
        var listItem = Assert.Single(page!.Items, p => p.ProjectCode == "PRJ-0001");

        var projectResponse = await client.GetAsync($"/api/projects/{listItem.Id}");
        var project = (await projectResponse.Content.ReadFromJsonAsync<ProjectDto>(Json))!;
        Assert.NotNull(project.CurrentRevisionId);

        var revisionResponse = await client.GetAsync($"/api/project-revisions/{project.CurrentRevisionId}");
        Assert.Equal(HttpStatusCode.OK, revisionResponse.StatusCode);
        var revision = (await revisionResponse.Content.ReadFromJsonAsync<ProjectRevisionDto>(Json))!;

        return (project, revision, client);
    }

    [Fact]
    public async Task Seeded_Reference_Project_Reproduces_The_Exact_Section20_Numbers_Over_Http()
    {
        var (_, revision, _) = await LoadSeededSampleProjectAsync();

        var line = Assert.Single(revision.Lines);
        Assert.Equal(3_030_000_000m, line.EquipmentCostPerPanel);
        Assert.Equal(3_030_000_000m, line.TotalCostPerPanel);
        Assert.Equal(3_030_000_000m, line.TotalLineCost);
        Assert.Equal(3_939_000_000m, line.SellingPricePerPanel);
        Assert.Equal(3_939_000_000m, line.TotalLineSellingPrice);
        Assert.Equal(590_850_000m, line.RialPayableAmount);
        Assert.Equal(909_000_000m, line.ProfitIrr);
        Assert.True(Math.Round(line.GrossMargin, 6) == 0.230769m);
        Assert.True(Math.Round(line.ForeignPayableAmount, 6) == 1_860.083333m);
        Assert.True(line.ReconciliationPassed);

        var totals = revision.Totals;
        Assert.Equal(3_030_000_000m, totals.TotalProjectCostIrr);
        Assert.Equal(3_939_000_000m, totals.TotalProjectSellingPriceIrr);
        Assert.Equal(590_850_000m, totals.TotalRialPayable);
        Assert.Equal(909_000_000m, totals.ProjectProfitIrr);
        Assert.True(totals.ReconciliationPassed);
        Assert.Empty(totals.ApprovalBlockers);
    }

    [Fact]
    public async Task Consolidated_Mto_Endpoint_Includes_Both_Bom_Items_And_Reconciles_To_The_Line_Cost()
    {
        var (_, revision, client) = await LoadSeededSampleProjectAsync();

        var response = await client.GetAsync($"/api/project-revisions/{revision.Id}/mto");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var mto = await response.Content.ReadFromJsonAsync<MtoResultDto>(Json);
        Assert.NotNull(mto);
        Assert.Equal(2, mto!.Electrical.Count); // ACB + Relay
        Assert.Equal(2, mto.Combined.Count);
        Assert.Equal(3_030_000_000m, mto.Combined.Sum(r => r.TotalProcurementCostIrr));
    }

    [Fact]
    public async Task Full_Approval_Workflow_Submit_Approve_Lock_Transitions_The_Revision_And_Records_History()
    {
        var (project, revision, client) = await LoadSeededSampleProjectAsync();

        // The seeded revision starts life in Draft (SampleProjectSeeder) — walk it through Section 21's state machine.
        var submitResponse = await client.PostAsJsonAsync($"/api/project-revisions/{revision.Id}/submit", new SubmitForApprovalRequest(revision.RowVersion), Json);
        Assert.Equal(HttpStatusCode.OK, submitResponse.StatusCode);
        var submitted = (await submitResponse.Content.ReadFromJsonAsync<ProjectRevisionDto>(Json))!;
        Assert.Equal("PendingApproval", submitted.Status);

        var decideResponse = await client.PostAsJsonAsync($"/api/project-revisions/{revision.Id}/decide",
            new ApprovalDecisionRequest(true, "Looks correct — approved by integration test.", submitted.RowVersion), Json);
        Assert.Equal(HttpStatusCode.OK, decideResponse.StatusCode);
        var approved = (await decideResponse.Content.ReadFromJsonAsync<ProjectRevisionDto>(Json))!;
        Assert.Equal("Approved", approved.Status);
        Assert.NotNull(approved.ApprovedByUserName);

        var lockResponse = await client.PostAsJsonAsync($"/api/project-revisions/{revision.Id}/lock", new LockRevisionRequest(approved.RowVersion), Json);
        Assert.Equal(HttpStatusCode.OK, lockResponse.StatusCode);
        var locked = (await lockResponse.Content.ReadFromJsonAsync<ProjectRevisionDto>(Json))!;
        Assert.Equal("Locked", locked.Status);

        var historyResponse = await client.GetAsync($"/api/project-revisions/{revision.Id}/approval-history");
        Assert.Equal(HttpStatusCode.OK, historyResponse.StatusCode);
        var history = await historyResponse.Content.ReadFromJsonAsync<IReadOnlyList<ApprovalHistoryItemDto>>(Json);
        Assert.Single(history!);
        Assert.True(history![0].IsApproved);
        Assert.Equal(3_939_000_000m, history[0].TotalProjectSellingPriceIrrAtDecision);

        // Section 13 — a Locked revision is immutable: adding a line to it must now be rejected.
        var attemptToModify = await client.PostAsJsonAsync($"/api/project-revisions/{revision.Id}/lines",
            new AddProjectLineRequest(Guid.NewGuid(), "C99", 1m, 0m), Json);
        Assert.False(attemptToModify.IsSuccessStatusCode);
    }
}
