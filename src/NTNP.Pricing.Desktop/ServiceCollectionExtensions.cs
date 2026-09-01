using Microsoft.Extensions.DependencyInjection;
using NTNP.Pricing.Desktop.Services;
using NTNP.Pricing.Desktop.Services.Api;
using NTNP.Pricing.Desktop.ViewModels;
using NTNP.Pricing.Desktop.Views;

namespace NTNP.Pricing.Desktop;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNtnpDesktop(this IServiceCollection services)
    {
        // Section 33 — the desktop client's one HTTP channel to the server. ApiClientBase resolves
        // the base address itself (from IServerConnectionSettingsService) on every call rather than
        // relying on HttpClient.BaseAddress, so one shared, long-lived HttpClient is safe here and
        // avoids IHttpClientFactory's per-typed-client pooling machinery for what is a single-server
        // desktop client.
        services.AddSingleton(_ => new HttpClient { Timeout = TimeSpan.FromSeconds(100) });

        services.AddSingleton<IServerConnectionSettingsService, ServerConnectionSettingsService>();
        services.AddSingleton<AppSession>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IDialogService, DialogService>();

        services.AddSingleton<AuthApiClient>();
        services.AddSingleton<HealthApiClient>();
        services.AddSingleton<CustomersApiClient>();
        services.AddSingleton<CurrenciesApiClient>();
        services.AddSingleton<EquipmentApiClient>();
        services.AddSingleton<LookupApiClient>();
        services.AddSingleton<PanelTemplatesApiClient>();
        services.AddSingleton<BodyEsTemplatesApiClient>();
        services.AddSingleton<PricingProfilesApiClient>();
        services.AddSingleton<ProjectsApiClient>();
        services.AddSingleton<ProjectRevisionsApiClient>();
        services.AddSingleton<UsersApiClient>();
        services.AddSingleton<AuditLogApiClient>();
        services.AddSingleton<DashboardApiClient>();
        services.AddSingleton<SettingsApiClient>();
        services.AddSingleton<ReportsApiClient>();
        services.AddSingleton<FilesApiClient>();

        // Windows — transient: a WPF Window cannot be re-shown after Close(), and a user can log out
        // and back in within the same process (Section 23's logout menu item), so both the shell and
        // login windows (and their view models) must be freshly resolved on each show.
        services.AddTransient<LoginWindow>();
        services.AddTransient<ShellWindow>();

        services.AddTransient<ShellViewModel>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<CustomersViewModel>();
        services.AddTransient<EquipmentViewModel>();
        services.AddTransient<CurrenciesViewModel>();
        services.AddTransient<PanelTemplatesViewModel>();
        services.AddTransient<BodyEsTemplatesViewModel>();
        services.AddTransient<PricingProfilesViewModel>();
        services.AddTransient<ProjectListViewModel>();
        services.AddTransient<NewProjectWizardViewModel>();
        services.AddTransient<ProjectWorkspaceViewModel>();
        services.AddTransient<UsersViewModel>();
        services.AddTransient<AuditLogViewModel>();
        services.AddTransient<CompanySettingsViewModel>();
        services.AddTransient<ServerConnectionSettingsViewModel>();

        return services;
    }
}
