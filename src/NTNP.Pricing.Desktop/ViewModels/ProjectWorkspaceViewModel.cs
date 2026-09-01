using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NTNP.Pricing.Contracts.Mto;
using NTNP.Pricing.Contracts.PanelTemplates;
using NTNP.Pricing.Contracts.Projects;
using NTNP.Pricing.Desktop.Services;
using NTNP.Pricing.Desktop.Services.Api;

namespace NTNP.Pricing.Desktop.ViewModels;

/// <summary>
/// Screens 13-20 (Section 22) — the workspace for one project, tabbed: Project Lineup, Generated
/// BOM, Consolidated MTO, Cost Breakdown, TOTAL, Project Revisions, Approval, Reports and Exports.
/// All eight operate on the same loaded <see cref="Revision"/>, which is exactly how the server
/// models it (one <see cref="ProjectRevisionDto"/> already carries lines, totals and approval state
/// together) — splitting them into eight separate screens that each re-fetch the same revision would
/// only add redundant round-trips, not real separation.
/// </summary>
public sealed partial class ProjectWorkspaceViewModel : ViewModelBase
{
    private readonly ProjectsApiClient _projectsApi;
    private readonly ProjectRevisionsApiClient _revisionsApi;
    private readonly PanelTemplatesApiClient _templatesApi;
    private readonly ReportsApiClient _reportsApi;
    private readonly IDialogService _dialogs;

    public Guid ProjectId { get; set; }

    [ObservableProperty] private ProjectDto? _project;
    [ObservableProperty] private ProjectRevisionDto? _revision;
    [ObservableProperty] private MtoResultDto? _mto;
    [ObservableProperty] private ProjectLineDto? _selectedLine;

    public ObservableCollection<RevisionListItemDto> RevisionHistory { get; } = new();
    public ObservableCollection<ApprovalHistoryItemDto> ApprovalHistory { get; } = new();
    public ObservableCollection<ProjectLineOverrideHistoryDto> OverrideHistory { get; } = new();

    public bool IsMutable => Revision is not null && Revision.Status is "Draft" or "UnderEngineeringReview" or "UnderCommercialReview";
    public bool CanSubmit => IsMutable;
    public bool CanDecide => Revision?.Status == "PendingApproval";
    public bool CanLock => Revision?.Status == "Approved";

    // --- Lineup (add-line) sub-form ---
    [ObservableProperty] private string _templateSearchText = string.Empty;
    [ObservableProperty] private PanelTemplateDto? _selectedTemplate;
    [ObservableProperty] private string _cellCode = string.Empty;
    [ObservableProperty] private decimal _quantityOfPanels = 1m;
    [ObservableProperty] private decimal _otherDirectCostPerPanel;
    public ObservableCollection<PanelTemplateDto> TemplateResults { get; } = new();

    // --- Override sub-form ---
    [ObservableProperty] private string _overrideFieldName = "SellingPricePerPanel";
    [ObservableProperty] private string _overrideNewValue = string.Empty;
    [ObservableProperty] private string _overrideReason = string.Empty;
    public static readonly IReadOnlyList<string> OverridableFields = new[] { "SellingPricePerPanel", "OtherDirectCostPerPanel", "PricingRateApplied" };

    // --- Approval sub-form ---
    [ObservableProperty] private string? _approvalComments;

    // --- Revisions/compare ---
    [ObservableProperty] private RevisionListItemDto? _compareFrom;
    [ObservableProperty] private RevisionListItemDto? _compareTo;
    [ObservableProperty] private RevisionComparisonDto? _comparison;

    // --- Reports ---
    [ObservableProperty] private string _quotationLanguage = "bilingual";

    public ProjectWorkspaceViewModel(
        ProjectsApiClient projectsApi, ProjectRevisionsApiClient revisionsApi, PanelTemplatesApiClient templatesApi,
        ReportsApiClient reportsApi, IDialogService dialogs)
    {
        _projectsApi = projectsApi;
        _revisionsApi = revisionsApi;
        _templatesApi = templatesApi;
        _reportsApi = reportsApi;
        _dialogs = dialogs;
    }

    public override Task OnNavigatedToAsync() => LoadAsync();

    [RelayCommand]
    private async Task LoadAsync() => await RunBusyAsync(LoadCoreAsync);

    // Unwrapped core so CreateNewRevisionAsync (already inside a RunBusyAsync scope) can reload the
    // project/revision-history without tripping RunBusyAsync's "already busy" reentrancy guard.
    private async Task LoadCoreAsync()
    {
        Project = await _projectsApi.GetAsync(ProjectId);
        if (Project.CurrentRevisionId is not null)
            await LoadRevisionAsync(Project.CurrentRevisionId.Value);

        var history = await _revisionsApi.ListForProjectAsync(ProjectId);
        RevisionHistory.Clear();
        foreach (var r in history) RevisionHistory.Add(r);
    }

    private async Task LoadRevisionAsync(Guid revisionId)
    {
        Revision = await _revisionsApi.GetAsync(revisionId);
        Mto = await _revisionsApi.GetMtoAsync(revisionId);

        var approvals = await _revisionsApi.GetApprovalHistoryAsync(revisionId);
        ApprovalHistory.Clear();
        foreach (var a in approvals) ApprovalHistory.Add(a);

        OnPropertyChanged(nameof(IsMutable));
        OnPropertyChanged(nameof(CanSubmit));
        OnPropertyChanged(nameof(CanDecide));
        OnPropertyChanged(nameof(CanLock));
    }

    [RelayCommand]
    private async Task OpenRevisionAsync(RevisionListItemDto? item)
    {
        if (item is null) return;
        await RunBusyAsync(() => LoadRevisionAsync(item.Id));
    }

    // ---------- Project Lineup / Generated BOM ----------

    [RelayCommand]
    private async Task SearchTemplatesAsync() => await RunBusyAsync(async () =>
    {
        var page = await _templatesApi.SearchAsync(TemplateSearchText, 1, 50, null, null);
        TemplateResults.Clear();
        foreach (var t in page.Items.Where(t => t.Status == "Approved")) TemplateResults.Add(t);
    });

    [RelayCommand]
    private async Task AddLineAsync() => await RunBusyAsync(async () =>
    {
        if (Revision is null || SelectedTemplate is null || string.IsNullOrWhiteSpace(CellCode)) return;
        Revision = await _revisionsApi.AddLineAsync(Revision.Id, new AddProjectLineRequest(SelectedTemplate.Id, CellCode, QuantityOfPanels, OtherDirectCostPerPanel));
        Mto = await _revisionsApi.GetMtoAsync(Revision.Id);
        CellCode = string.Empty;
        QuantityOfPanels = 1m;
        OtherDirectCostPerPanel = 0m;
    });

    [RelayCommand]
    private async Task RemoveLineAsync(ProjectLineDto? line) => await RunBusyAsync(async () =>
    {
        if (Revision is null || line is null) return;
        if (!_dialogs.Confirm("حذف ردیف", $"آیا ردیف «{line.CellCode}» حذف شود؟")) return;
        Revision = await _revisionsApi.RemoveLineAsync(Revision.Id, line.Id, Revision.RowVersion);
        Mto = await _revisionsApi.GetMtoAsync(Revision.Id);
    });

    [RelayCommand]
    private async Task UpdateLineQuantityAsync(ProjectLineDto? line) => await RunBusyAsync(async () =>
    {
        if (Revision is null || line is null) return;
        Revision = await _revisionsApi.UpdateLineQuantityAsync(Revision.Id, line.Id, new UpdateProjectLineQuantityRequest(line.QuantityOfPanels, line.OtherDirectCostPerPanel, Revision.RowVersion));
        Mto = await _revisionsApi.GetMtoAsync(Revision.Id);
    });

    [RelayCommand]
    private async Task OverrideLineAsync() => await RunBusyAsync(async () =>
    {
        if (Revision is null || SelectedLine is null) return;
        if (string.IsNullOrWhiteSpace(OverrideReason))
        {
            ErrorMessage = "دلیل اصلاحیه الزامی است (بند ۱۴).";
            return;
        }
        Revision = await _revisionsApi.OverrideLineFieldAsync(Revision.Id, SelectedLine.Id, new ProjectLineOverrideRequest(OverrideFieldName, OverrideNewValue, OverrideReason, Revision.RowVersion));
        OverrideNewValue = string.Empty;
        OverrideReason = string.Empty;
        SelectedLine = Revision.Lines.FirstOrDefault(l => l.Id == SelectedLine.Id);
        if (SelectedLine is not null) await LoadOverrideHistoryAsync(SelectedLine.Id);
    });

    [RelayCommand]
    private async Task LoadOverrideHistoryAsync(Guid lineId) => await RunBusyAsync(async () =>
    {
        var history = await _revisionsApi.GetOverrideHistoryAsync(lineId);
        OverrideHistory.Clear();
        foreach (var h in history) OverrideHistory.Add(h);
    });

    partial void OnSelectedLineChanged(ProjectLineDto? value)
    {
        OverrideHistory.Clear();
        if (value is not null) _ = LoadOverrideHistoryAsync(value.Id);
    }

    // ---------- Revisions ----------

    [RelayCommand]
    private async Task CreateNewRevisionAsync() => await RunBusyAsync(async () =>
    {
        var revision = await _revisionsApi.CreateNewRevisionUsingLatestPricesAsync(ProjectId);
        await LoadCoreAsync();
        await LoadRevisionAsync(revision.Id);
    });

    [RelayCommand]
    private async Task CompareRevisionsAsync() => await RunBusyAsync(async () =>
    {
        if (CompareFrom is null || CompareTo is null) return;
        Comparison = await _revisionsApi.CompareAsync(CompareFrom.Id, CompareTo.Id);
    });

    // ---------- Approval ----------

    [RelayCommand]
    private async Task SubmitForApprovalAsync() => await RunBusyAsync(async () =>
    {
        if (Revision is null) return;
        Revision = await _revisionsApi.SubmitForApprovalAsync(Revision.Id, new SubmitForApprovalRequest(Revision.RowVersion));
        OnPropertyChanged(nameof(IsMutable)); OnPropertyChanged(nameof(CanSubmit)); OnPropertyChanged(nameof(CanDecide)); OnPropertyChanged(nameof(CanLock));
    });

    [RelayCommand]
    private async Task ApproveAsync() => await DecideAsync(true);

    [RelayCommand]
    private async Task RejectAsync() => await DecideAsync(false);

    private async Task DecideAsync(bool approve) => await RunBusyAsync(async () =>
    {
        if (Revision is null) return;
        Revision = await _revisionsApi.DecideApprovalAsync(Revision.Id, new ApprovalDecisionRequest(approve, ApprovalComments, Revision.RowVersion));
        var approvals = await _revisionsApi.GetApprovalHistoryAsync(Revision.Id);
        ApprovalHistory.Clear();
        foreach (var a in approvals) ApprovalHistory.Add(a);
        ApprovalComments = null;
        OnPropertyChanged(nameof(IsMutable)); OnPropertyChanged(nameof(CanSubmit)); OnPropertyChanged(nameof(CanDecide)); OnPropertyChanged(nameof(CanLock));
    });

    [RelayCommand]
    private async Task LockAsync() => await RunBusyAsync(async () =>
    {
        if (Revision is null) return;
        if (!_dialogs.Confirm("قفل کردن نسخه", "پس از قفل شدن، این نسخه دیگر غیرقابل ویرایش خواهد بود. ادامه می‌دهید؟")) return;
        Revision = await _revisionsApi.LockAsync(Revision.Id, new LockRevisionRequest(Revision.RowVersion));
        OnPropertyChanged(nameof(IsMutable)); OnPropertyChanged(nameof(CanSubmit)); OnPropertyChanged(nameof(CanDecide)); OnPropertyChanged(nameof(CanLock));
    });

    // ---------- Reports and Exports ----------

    [RelayCommand]
    private async Task ExportQuotationAsync() => await ExportAsync(() => _reportsApi.GetQuotationPdfAsync(Revision!.Id, QuotationLanguage));

    [RelayCommand]
    private async Task ExportInternalCostingPdfAsync() => await ExportAsync(() => _reportsApi.GetInternalCostingAsync(Revision!.Id, "pdf"));

    [RelayCommand]
    private async Task ExportInternalCostingExcelAsync() => await ExportAsync(() => _reportsApi.GetInternalCostingAsync(Revision!.Id, "xlsx"));

    [RelayCommand]
    private async Task ExportMtoPdfAsync(string? kind) => await ExportAsync(() => _reportsApi.GetMtoAsync(Revision!.Id, kind ?? "combined", "pdf"));

    [RelayCommand]
    private async Task ExportMtoExcelAsync(string? kind) => await ExportAsync(() => _reportsApi.GetMtoAsync(Revision!.Id, kind ?? "combined", "xlsx"));

    private async Task ExportAsync(Func<Task<(byte[] Bytes, string? FileName, string ContentType)>> fetch) => await RunBusyAsync(async () =>
    {
        if (Revision is null) return;
        var (bytes, suggestedName, _) = await fetch();
        var extension = Path.GetExtension(suggestedName) is { Length: > 0 } ext ? ext : ".pdf";
        var filter = extension == ".xlsx" ? "Excel Workbook (*.xlsx)|*.xlsx" : "PDF Document (*.pdf)|*.pdf";
        var path = _dialogs.ShowSaveFileDialog(suggestedName ?? $"report{extension}", filter);
        if (path is null) return;
        await File.WriteAllBytesAsync(path, bytes);
        _dialogs.ShowInfo("ذخیره شد", $"فایل با موفقیت در مسیر زیر ذخیره شد:\n{path}");
    });
}
