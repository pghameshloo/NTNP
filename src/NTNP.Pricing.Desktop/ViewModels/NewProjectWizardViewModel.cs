using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using NTNP.Pricing.Contracts.Customers;
using NTNP.Pricing.Contracts.PanelTemplates;
using NTNP.Pricing.Contracts.PricingProfiles;
using NTNP.Pricing.Contracts.Projects;
using NTNP.Pricing.Desktop.Services;
using NTNP.Pricing.Desktop.Services.Api;

namespace NTNP.Pricing.Desktop.ViewModels;

/// <summary>
/// Screen 12 (Section 22) — New Project Wizard. Maps the master prompt's 8 conceptual steps onto 4
/// real UI steps: Section 15's Automatic BOM Generator computes BOM + BODY+ES + cost from a single
/// panel-template selection, so "Panel Selection", "BOM Generation" and "BODY+ES" are one step here
/// (there is no separate manual data-entry surface for them — that would be re-implementing the
/// Excel-era manual process the generator replaces). "Approval and Issue" is not duplicated here
/// either: it is Section 21's workflow, owned by <see cref="ProjectWorkspaceViewModel"/>'s Approval
/// tab, which this wizard hands off to on Finish.
/// </summary>
public sealed partial class NewProjectWizardViewModel : ViewModelBase
{
    private readonly ProjectsApiClient _projectsApi;
    private readonly ProjectRevisionsApiClient _revisionsApi;
    private readonly CustomersApiClient _customersApi;
    private readonly PricingProfilesApiClient _profilesApi;
    private readonly PanelTemplatesApiClient _templatesApi;
    private readonly INavigationService _navigation;

    public static readonly IReadOnlyList<string> Steps = new[] { "اطلاعات پروژه", "انتخاب تابلو و تولید BOM", "TOTAL", "بازبینی و تکمیل" };

    [ObservableProperty] private int _currentStep;
    [ObservableProperty] private ProjectDto? _createdProject;
    [ObservableProperty] private ProjectRevisionDto? _currentRevision;

    // Step 1 — project info + pricing settings (CreateProjectRequest needs both at once).
    [ObservableProperty] private string _projectCode = string.Empty;
    [ObservableProperty] private string _projectName = string.Empty;
    [ObservableProperty] private CustomerDto? _customer;
    [ObservableProperty] private string? _rfqNumber;
    [ObservableProperty] private DateTimeOffset? _inquiryDate = DateTimeOffset.Now;
    [ObservableProperty] private string? _projectDescription;
    [ObservableProperty] private string? _commercialNotes;
    [ObservableProperty] private string? _technicalNotes;
    [ObservableProperty] private string _quotationCurrencyCode = "EUR";
    [ObservableProperty] private decimal _rialShare = 0.15m;
    [ObservableProperty] private decimal _foreignShare = 0.85m;
    [ObservableProperty] private PricingProfileDto? _pricingProfile;
    [ObservableProperty] private string _pricingMethod = "Markup";
    [ObservableProperty] private decimal _pricingRate = 0.3m;

    // Step 2 — panel lines
    [ObservableProperty] private string _templateSearchText = string.Empty;
    [ObservableProperty] private PanelTemplateDto? _selectedTemplate;
    [ObservableProperty] private string _cellCode = string.Empty;
    [ObservableProperty] private decimal _quantityOfPanels = 1m;
    [ObservableProperty] private decimal _otherDirectCostPerPanel;

    public ObservableCollection<CustomerDto> Customers { get; } = new();
    public ObservableCollection<PricingProfileDto> PricingProfiles { get; } = new();
    public ObservableCollection<PanelTemplateDto> TemplateResults { get; } = new();

    public NewProjectWizardViewModel(
        ProjectsApiClient projectsApi, ProjectRevisionsApiClient revisionsApi, CustomersApiClient customersApi,
        PricingProfilesApiClient profilesApi, PanelTemplatesApiClient templatesApi, INavigationService navigation)
    {
        _projectsApi = projectsApi;
        _revisionsApi = revisionsApi;
        _customersApi = customersApi;
        _profilesApi = profilesApi;
        _templatesApi = templatesApi;
        _navigation = navigation;
    }

    public override async Task OnNavigatedToAsync()
    {
        CurrentStep = 0;
        CreatedProject = null;
        CurrentRevision = null;

        await RunBusyAsync(async () =>
        {
            var customers = await _customersApi.SearchAsync(null, 1, 500, false);
            Customers.Clear();
            foreach (var c in customers.Items) Customers.Add(c);

            var profiles = await _profilesApi.ListAsync();
            PricingProfiles.Clear();
            foreach (var p in profiles) PricingProfiles.Add(p);
        });
    }

    partial void OnPricingProfileChanged(PricingProfileDto? value)
    {
        if (value is null) return;
        QuotationCurrencyCode = value.DefaultQuotationCurrencyCode;
        RialShare = value.DefaultRialShare;
        ForeignShare = value.DefaultForeignShare;
        PricingMethod = value.PricingMethod;
        PricingRate = value.DefaultRate;
    }

    [RelayCommand]
    private async Task CreateProjectAsync() => await RunBusyAsync(async () =>
    {
        if (Customer is null)
        {
            ErrorMessage = "انتخاب مشتری الزامی است.";
            return;
        }

        var project = await _projectsApi.CreateAsync(new CreateProjectRequest(
            ProjectCode, ProjectName, Customer.Id, RfqNumber, DateOnly.FromDateTime((InquiryDate ?? DateTimeOffset.Now).DateTime),
            ProjectDescription, CommercialNotes, TechnicalNotes, QuotationCurrencyCode, RialShare, ForeignShare,
            PricingProfile?.Id, PricingMethod, PricingRate));

        CreatedProject = project;
        CurrentRevision = project.CurrentRevisionId is null ? null : await _revisionsApi.GetAsync(project.CurrentRevisionId.Value);
        CurrentStep = 1;
    });

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
        if (CurrentRevision is null || SelectedTemplate is null || string.IsNullOrWhiteSpace(CellCode))
        {
            ErrorMessage = "انتخاب تیپ تابلو و وارد کردن کد سلول الزامی است.";
            return;
        }

        CurrentRevision = await _revisionsApi.AddLineAsync(CurrentRevision.Id, new AddProjectLineRequest(SelectedTemplate.Id, CellCode, QuantityOfPanels, OtherDirectCostPerPanel));
        CellCode = string.Empty;
        QuantityOfPanels = 1m;
        OtherDirectCostPerPanel = 0m;
    });

    [RelayCommand] private void GoToTotalsStep() => CurrentStep = 2;
    [RelayCommand] private void GoToReviewStep() => CurrentStep = 3;
    [RelayCommand] private void GoBack() => CurrentStep = Math.Max(0, CurrentStep - 1);

    [RelayCommand]
    private async Task FinishAsync()
    {
        if (CreatedProject is null) return;
        var workspace = App.Services.GetRequiredService<ProjectWorkspaceViewModel>();
        workspace.ProjectId = CreatedProject.Id;
        await _navigation.NavigateToAsync(workspace, $"{CreatedProject.ProjectCode} — {CreatedProject.ProjectName}", "پروژه‌ها");
    }
}
