using NTNP.Pricing.Contracts.Mto;
using NTNP.Pricing.Contracts.Projects;
using NTNP.Pricing.Desktop.Services.Api;
using NTNP.Pricing.Desktop.Tests.TestSupport;
using NTNP.Pricing.Desktop.ViewModels;

namespace NTNP.Pricing.Desktop.Tests;

/// <summary>Section 21's approval state machine, exercised through the real view model + fake HTTP layer.</summary>
public class ProjectWorkspaceViewModelTests
{
    private static readonly Guid ProjectId = Guid.NewGuid();
    private static readonly Guid RevisionId = Guid.NewGuid();

    // Named arguments throughout — both DTOs have 20+ positional fields, and a miscounted null run
    // is exactly the kind of mistake positional construction hides until a confusing runtime failure.
    private static ProjectDto MakeProject() => new(
        Id: ProjectId, ProjectCode: "PRJ-0001", ProjectName: "Sample Project", CustomerId: Guid.NewGuid(), CustomerName: "Sample Customer",
        RfqNumber: null, InquiryDate: null, QuotationNumber: null, QuotationDate: null, QuotationValidUntil: null,
        ProjectDescription: null, CommercialNotes: null, TechnicalNotes: null,
        QuotationCurrencyCode: "EUR", RialShare: 0.15m, ForeignShare: 0.85m, PricingProfileId: null, PricingMethod: "Markup", PricingRate: 0.30m,
        Status: "Draft", CurrentRevisionNumber: 1, CurrentRevisionId: RevisionId, CreatedByUserName: "tester", CreatedAtUtc: DateTimeOffset.UtcNow,
        RowVersion: new byte[] { 1 });

    private static ProjectRevisionDto MakeRevision(string status = "Draft", byte[]? rowVersion = null) => new(
        Id: RevisionId, ProjectId: ProjectId, RevisionNumber: 1, Status: status, QuotationCurrencyCode: "EUR",
        RialShare: 0.15m, ForeignShare: 0.85m, PricingMethod: "Markup", PricingRate: 0.30m,
        SellingExchangeRateValue: 1_800_000m, SellingExchangeRateEffectiveAtUtc: DateTimeOffset.UtcNow,
        Lines: Array.Empty<ProjectLineDto>(),
        Totals: new ProjectRevisionTotalsDto(
            TotalEquipmentCostIrr: 0, TotalBodyEsCostIrr: 0, TotalOtherDirectCostIrr: 0, TotalProjectCostIrr: 0,
            TotalProjectSellingPriceIrr: 0, TotalRialPayable: 0, QuotationCurrencyCode: "EUR", SellingExchangeRateValue: 1_800_000m,
            TotalForeignPayable: 0, ProjectProfitIrr: 0, ProjectGrossMargin: 0, ReconciliationDifferenceIrr: 0,
            ReconciliationPassed: true, ApprovalBlockers: Array.Empty<string>()),
        SubmittedByUserName: null, SubmittedAtUtc: null, ApprovedByUserName: null, ApprovedAtUtc: null, RejectionReason: null,
        RowVersion: rowVersion ?? new byte[] { 1 });

    private static (ProjectWorkspaceViewModel ViewModel, FakeHttpMessageHandler Handler) Build()
    {
        var handler = new FakeHttpMessageHandler();
        var http = new HttpClient(handler);
        var serverSettings = new FakeServerConnectionSettingsService();
        var session = AuthenticatedSessionFactory.Create();
        var vm = new ProjectWorkspaceViewModel(
            new ProjectsApiClient(http, serverSettings, session), new ProjectRevisionsApiClient(http, serverSettings, session),
            new PanelTemplatesApiClient(http, serverSettings, session), new ReportsApiClient(http, serverSettings, session),
            new FakeDialogService()) { ProjectId = ProjectId };
        return (vm, handler);
    }

    private static void RegisterCommonRoutes(FakeHttpMessageHandler handler, ProjectRevisionDto revision)
    {
        handler.WhenJson(HttpMethod.Get, $"api/projects/{ProjectId}", MakeProject());
        handler.WhenJson(HttpMethod.Get, $"api/project-revisions/{RevisionId}", revision);
        handler.WhenJson(HttpMethod.Get, $"api/projects/{ProjectId}/revisions", Array.Empty<RevisionListItemDto>());
        handler.WhenJson(HttpMethod.Get, $"api/project-revisions/{RevisionId}/mto", new MtoResultDto(Array.Empty<MtoLineDto>(), Array.Empty<MtoLineDto>(), Array.Empty<MtoLineDto>()));
        handler.WhenJson(HttpMethod.Get, $"api/project-revisions/{RevisionId}/approval-history", Array.Empty<ApprovalHistoryItemDto>());
    }

    [Fact]
    public async Task Load_PopulatesProjectAndRevision_AndComputesMutabilityFlags()
    {
        var (vm, handler) = Build();
        RegisterCommonRoutes(handler, MakeRevision("Draft"));

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.NotNull(vm.Project);
        Assert.Equal("PRJ-0001", vm.Project!.ProjectCode);
        Assert.True(vm.IsMutable);
        Assert.True(vm.CanSubmit);
        Assert.False(vm.CanDecide);
        Assert.False(vm.CanLock);
    }

    [Fact]
    public async Task SubmitForApproval_TransitionsToPendingApproval_AndFlipsTheActionButtons()
    {
        var (vm, handler) = Build();
        RegisterCommonRoutes(handler, MakeRevision("Draft"));
        await vm.LoadCommand.ExecuteAsync(null);

        handler.WhenJson(HttpMethod.Post, $"api/project-revisions/{RevisionId}/submit", MakeRevision("PendingApproval"));

        await vm.SubmitForApprovalCommand.ExecuteAsync(null);

        Assert.Equal("PendingApproval", vm.Revision!.Status);
        Assert.False(vm.CanSubmit);
        Assert.True(vm.CanDecide);
        Assert.False(vm.CanLock);
    }

    [Fact]
    public async Task Approve_TransitionsToApproved_AndLockBecomesAvailable()
    {
        var (vm, handler) = Build();
        RegisterCommonRoutes(handler, MakeRevision("PendingApproval"));
        await vm.LoadCommand.ExecuteAsync(null);

        handler.WhenJson(HttpMethod.Post, $"api/project-revisions/{RevisionId}/decide", MakeRevision("Approved"));
        handler.WhenJson(HttpMethod.Get, $"api/project-revisions/{RevisionId}/approval-history",
            new[] { new ApprovalHistoryItemDto(Guid.NewGuid(), true, "looks good", "approver", DateTimeOffset.UtcNow, 0, 0, 0) });

        await vm.ApproveCommand.ExecuteAsync(null);

        Assert.Equal("Approved", vm.Revision!.Status);
        Assert.False(vm.CanDecide);
        Assert.True(vm.CanLock);
        Assert.Single(vm.ApprovalHistory);
    }

    [Fact]
    public async Task Lock_WhenUserConfirms_TransitionsToLocked_AndTheRevisionIsNoLongerMutable()
    {
        var (vm, handler) = Build();
        RegisterCommonRoutes(handler, MakeRevision("Approved"));
        await vm.LoadCommand.ExecuteAsync(null);

        handler.WhenJson(HttpMethod.Post, $"api/project-revisions/{RevisionId}/lock", MakeRevision("Locked"));

        await vm.LockCommand.ExecuteAsync(null);

        Assert.Equal("Locked", vm.Revision!.Status);
        Assert.False(vm.IsMutable);
        Assert.False(vm.CanLock);
    }

    [Fact]
    public async Task AddLine_WithoutASelectedTemplate_DoesNothing_RatherThanSendingAnInvalidRequest()
    {
        var (vm, handler) = Build();
        RegisterCommonRoutes(handler, MakeRevision("Draft"));
        await vm.LoadCommand.ExecuteAsync(null);
        var requestCountBefore = handler.Requests.Count;

        vm.CellCode = "C01";
        await vm.AddLineCommand.ExecuteAsync(null); // SelectedTemplate is still null

        Assert.Equal(requestCountBefore, handler.Requests.Count);
    }
}
