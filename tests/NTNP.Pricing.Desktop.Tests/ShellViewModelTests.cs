using NTNP.Pricing.Contracts.Common;
using NTNP.Pricing.Contracts.Dashboard;
using NTNP.Pricing.Desktop.Services;
using NTNP.Pricing.Desktop.Services.Api;
using NTNP.Pricing.Desktop.Tests.TestSupport;
using NTNP.Pricing.Desktop.ViewModels;

namespace NTNP.Pricing.Desktop.Tests;

public class ShellViewModelTests
{
    private static (ShellViewModel ViewModel, FakeHttpMessageHandler Handler, AppSession Session) Build()
    {
        var handler = new FakeHttpMessageHandler();
        var http = new HttpClient(handler);
        var serverSettings = new FakeServerConnectionSettingsService();
        var session = AuthenticatedSessionFactory.Create();
        var services = new ServiceCollectionForShellTests(http, serverSettings, session);
        var navigation = new NavigationService(services);
        var authApi = new AuthApiClient(http, serverSettings, session);
        return (new ShellViewModel(navigation, session, authApi), handler, session);
    }

    [Fact]
    public void ToggleNav_FlipsIsNavCollapsed()
    {
        var (vm, _, _) = Build();

        vm.ToggleNavCommand.Execute(null);
        Assert.True(vm.IsNavCollapsed);

        vm.ToggleNavCommand.Execute(null);
        Assert.False(vm.IsNavCollapsed);
    }

    [Fact]
    public async Task NavigateDashboard_SetsTheCurrentModuleAndLoadsTheDashboardViewModel()
    {
        var (vm, handler, _) = Build();
        handler.WhenJson(HttpMethod.Get, "api/dashboard/summary", new DashboardSummaryDto(
            0, 0, 0, 0, 0, 0, 0m, 0m, Array.Empty<RecentProjectDto>(), Array.Empty<QuotationValuePointDto>(),
            Array.Empty<StatusCountDto>(), Array.Empty<CostCompositionDto>(), Array.Empty<RecentPriceChangeDto>()));

        await vm.NavigateDashboardCommand.ExecuteAsync(null);

        Assert.Equal("داشبورد", vm.Navigation.CurrentModuleTitle);
        Assert.IsType<DashboardViewModel>(vm.Navigation.CurrentViewModel);
    }

    [Fact]
    public async Task NavigateCustomers_SetsModuleGroupAndTitle()
    {
        var (vm, handler, _) = Build();
        handler.WhenJson(HttpMethod.Get, "api/customers", new PagedResult<NTNP.Pricing.Contracts.Customers.CustomerDto>(Array.Empty<NTNP.Pricing.Contracts.Customers.CustomerDto>(), 0, 1, 200));

        await vm.NavigateCustomersCommand.ExecuteAsync(null);

        Assert.Equal("مشتریان", vm.Navigation.CurrentModuleTitle);
        Assert.Equal("اطلاعات پایه", vm.Navigation.CurrentModuleGroup);
    }

    [Fact]
    public async Task Logout_ClearsTheSessionAndRaisesLoggedOut()
    {
        var (vm, handler, session) = Build();
        handler.When(HttpMethod.Post, "api/auth/logout", _ => new HttpResponseMessage(System.Net.HttpStatusCode.NoContent));

        var raised = false;
        vm.LoggedOut += () => raised = true;

        Assert.True(session.IsAuthenticated);
        await vm.LogoutCommand.ExecuteAsync(null);

        Assert.True(raised);
        Assert.False(session.IsAuthenticated);
        Assert.Null(session.CurrentUser);
    }
}

/// <summary>
/// A minimal IServiceProvider that resolves only the view models ShellViewModelTests actually
/// navigates to, wired with the same fake HttpClient/session as the test — enough for
/// NavigationService.NavigateToAsync&lt;T&gt;() without pulling in the full app composition root.
/// </summary>
internal sealed class ServiceCollectionForShellTests : IServiceProvider
{
    private readonly HttpClient _http;
    private readonly IServerConnectionSettingsService _serverSettings;
    private readonly AppSession _session;

    public ServiceCollectionForShellTests(HttpClient http, IServerConnectionSettingsService serverSettings, AppSession session)
    {
        _http = http;
        _serverSettings = serverSettings;
        _session = session;
    }

    public object? GetService(Type serviceType)
    {
        if (serviceType == typeof(DashboardViewModel))
            return new DashboardViewModel(new DashboardApiClient(_http, _serverSettings, _session));
        if (serviceType == typeof(CustomersViewModel))
            return new CustomersViewModel(new CustomersApiClient(_http, _serverSettings, _session), new FakeDialogService());
        throw new NotSupportedException($"{serviceType.Name} is not wired for this test's fake service provider.");
    }
}
