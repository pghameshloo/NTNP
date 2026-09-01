using NTNP.Pricing.Contracts.Common;
using NTNP.Pricing.Contracts.Customers;
using NTNP.Pricing.Contracts.PanelTemplates;
using NTNP.Pricing.Contracts.PricingProfiles;
using NTNP.Pricing.Contracts.Projects;
using NTNP.Pricing.Desktop.Services;
using NTNP.Pricing.Desktop.Services.Api;
using NTNP.Pricing.Desktop.Tests.TestSupport;
using NTNP.Pricing.Desktop.ViewModels;

namespace NTNP.Pricing.Desktop.Tests;

/// <summary>Section 22 screen 12 — the four-step wizard (see the class-level comment on NewProjectWizardViewModel for why it's four, not eight).</summary>
public class NewProjectWizardViewModelTests
{
    private static readonly Guid RevisionId = Guid.NewGuid();
    private static readonly Guid ProjectId = Guid.NewGuid();

    private static (NewProjectWizardViewModel ViewModel, FakeHttpMessageHandler Handler) Build()
    {
        var handler = new FakeHttpMessageHandler();
        var http = new HttpClient(handler);
        var serverSettings = new FakeServerConnectionSettingsService();
        var session = AuthenticatedSessionFactory.Create();
        var navigation = new NavigationService(new ThrowingServiceProvider());
        var vm = new NewProjectWizardViewModel(
            new ProjectsApiClient(http, serverSettings, session), new ProjectRevisionsApiClient(http, serverSettings, session),
            new CustomersApiClient(http, serverSettings, session), new PricingProfilesApiClient(http, serverSettings, session),
            new PanelTemplatesApiClient(http, serverSettings, session), navigation);
        return (vm, handler);
    }

    [Fact]
    public async Task OnNavigatedToAsync_ResetsToStepZero_AndLoadsPickerData()
    {
        var (vm, handler) = Build();
        handler.WhenJson(HttpMethod.Get, "api/customers", new PagedResult<CustomerDto>(Array.Empty<CustomerDto>(), 0, 1, 500));
        handler.WhenJson(HttpMethod.Get, "api/pricing-profiles", Array.Empty<PricingProfileDto>());

        vm.CurrentStep = 3; // simulate a leftover state from a previous run

        await vm.OnNavigatedToAsync();

        Assert.Equal(0, vm.CurrentStep);
        Assert.Null(vm.CreatedProject);
    }

    [Fact]
    public void SelectingAPricingProfile_AppliesItsDefaultsToTheForm()
    {
        var (vm, _) = Build();
        var profile = new PricingProfileDto(Guid.NewGuid(), "Standard MV", "GrossMargin", 0.25m, 1m, 0.20m, 0.80m, "USD", "NearestThousand", "NearestInteger", 2, 1m, true, new byte[] { 1 });

        vm.PricingProfile = profile;

        Assert.Equal("USD", vm.QuotationCurrencyCode);
        Assert.Equal(0.20m, vm.RialShare);
        Assert.Equal(0.80m, vm.ForeignShare);
        Assert.Equal("GrossMargin", vm.PricingMethod);
        Assert.Equal(0.25m, vm.PricingRate);
    }

    [Fact]
    public async Task CreateProjectAsync_WithoutACustomer_SetsAnErrorMessage_AndNeverCallsTheApi()
    {
        var (vm, handler) = Build();
        vm.ProjectCode = "PRJ-TEST";
        vm.ProjectName = "Test Project";
        // Customer left null on purpose.

        await vm.CreateProjectCommand.ExecuteAsync(null);

        Assert.NotNull(vm.ErrorMessage);
        Assert.Equal(0, vm.CurrentStep); // still on step 1 — never advanced
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task CreateProjectAsync_Succeeds_AdvancesToStepTwo_AndLoadsTheNewRevision()
    {
        var (vm, handler) = Build();
        vm.Customer = new CustomerDto(Guid.NewGuid(), "CUST-001", "Acme", null, null, null, null, null, null, null, null, null, true, "admin", DateTimeOffset.UtcNow, null, null, new byte[] { 1 });
        vm.ProjectCode = "PRJ-TEST";
        vm.ProjectName = "Test Project";

        var project = new ProjectDto(
            ProjectId, "PRJ-TEST", "Test Project", vm.Customer.Id, "Acme", null, null, null, null, null, null, null, null,
            "EUR", 0.15m, 0.85m, null, "Markup", 0.30m, "Draft", 1, RevisionId, "tester", DateTimeOffset.UtcNow, new byte[] { 1 });
        var revision = new ProjectRevisionDto(
            RevisionId, ProjectId, 1, "Draft", "EUR", 0.15m, 0.85m, "Markup", 0.30m, 1_800_000m, DateTimeOffset.UtcNow,
            Array.Empty<ProjectLineDto>(),
            new ProjectRevisionTotalsDto(0, 0, 0, 0, 0, 0, "EUR", 1_800_000m, 0, 0, 0, 0, true, Array.Empty<string>()),
            null, null, null, null, null, new byte[] { 1 });

        handler.WhenJson(HttpMethod.Post, "api/projects", project);
        handler.WhenJson(HttpMethod.Get, $"api/project-revisions/{RevisionId}", revision);

        await vm.CreateProjectCommand.ExecuteAsync(null);

        Assert.Equal(1, vm.CurrentStep);
        Assert.Equal("PRJ-TEST", vm.CreatedProject!.ProjectCode);
        Assert.NotNull(vm.CurrentRevision);
    }

    [Fact]
    public void GoBack_NeverGoesBelowStepZero()
    {
        var (vm, _) = Build();
        vm.CurrentStep = 0;

        vm.GoBackCommand.Execute(null);

        Assert.Equal(0, vm.CurrentStep);
    }

    private sealed class ThrowingServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => throw new NotSupportedException("The wizard's own tests never navigate away from itself.");
    }
}
