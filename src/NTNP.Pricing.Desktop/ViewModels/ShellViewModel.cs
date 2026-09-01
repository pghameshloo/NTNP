using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using NTNP.Pricing.Contracts.Auth;
using NTNP.Pricing.Desktop.Services;
using NTNP.Pricing.Desktop.Services.Api;

namespace NTNP.Pricing.Desktop.ViewModels;

/// <summary>Section 23 — the application shell: header, collapsible RTL nav, and the content area that hosts every other screen.</summary>
public sealed partial class ShellViewModel : ViewModelBase
{
    private readonly INavigationService _navigation;
    private readonly AuthApiClient _authApi;

    [ObservableProperty] private bool _isNavCollapsed;

    public AppSession Session { get; }
    public INavigationService Navigation => _navigation;

    /// <summary>Raised after the session has been cleared — the shell window closes itself and the app shows Login again.</summary>
    public event Action? LoggedOut;

    public ShellViewModel(INavigationService navigation, AppSession session, AuthApiClient authApi)
    {
        _navigation = navigation;
        Session = session;
        _authApi = authApi;
    }

    [RelayCommand] private void ToggleNav() => IsNavCollapsed = !IsNavCollapsed;

    [RelayCommand] private Task NavigateDashboard() => _navigation.NavigateToAsync<DashboardViewModel>("داشبورد");
    [RelayCommand] private Task NavigateCustomers() => _navigation.NavigateToAsync<CustomersViewModel>("مشتریان", "اطلاعات پایه");
    [RelayCommand] private Task NavigateEquipment() => _navigation.NavigateToAsync<EquipmentViewModel>("بانک تجهیزات", "اطلاعات پایه");
    [RelayCommand] private Task NavigateCurrencies() => _navigation.NavigateToAsync<CurrenciesViewModel>("نرخ ارز", "اطلاعات پایه");
    [RelayCommand] private Task NavigatePanelTemplates() => _navigation.NavigateToAsync<PanelTemplatesViewModel>("تیپ تابلوها", "اطلاعات پایه");
    [RelayCommand] private Task NavigateBodyEs() => _navigation.NavigateToAsync<BodyEsTemplatesViewModel>("BODY+ES", "اطلاعات پایه");
    [RelayCommand] private Task NavigatePricingProfiles() => _navigation.NavigateToAsync<PricingProfilesViewModel>("پروفایل قیمت‌گذاری", "اطلاعات پایه");

    [RelayCommand] private Task NavigateProjectList() => _navigation.NavigateToAsync<ProjectListViewModel>("پروژه‌های قیمت‌گذاری", "پروژه‌ها");
    [RelayCommand] private Task NavigateNewProject() => _navigation.NavigateToAsync<NewProjectWizardViewModel>("پروژه جدید", "پروژه‌ها");

    [RelayCommand]
    private Task NavigateApprovalQueue()
    {
        var vm = App.Services.GetRequiredService<ProjectListViewModel>();
        vm.StatusFilter = "PendingApproval";
        return _navigation.NavigateToAsync(vm, "بررسی و تأیید", "پروژه‌ها");
    }

    [RelayCommand] private Task NavigateUsers() => _navigation.NavigateToAsync<UsersViewModel>("کاربران", "مدیریت سیستم");
    [RelayCommand] private Task NavigateAuditLog() => _navigation.NavigateToAsync<AuditLogViewModel>("گزارش فعالیت‌ها", "مدیریت سیستم");
    [RelayCommand] private Task NavigateCompanySettings() => _navigation.NavigateToAsync<CompanySettingsViewModel>("تنظیمات", "مدیریت سیستم");
    [RelayCommand] private Task NavigateServerSettings() => _navigation.NavigateToAsync<ServerConnectionSettingsViewModel>("تنظیمات اتصال به سرور", "مدیریت سیستم");

    [RelayCommand]
    private async Task LogoutAsync()
    {
        try
        {
            if (!string.IsNullOrEmpty(Session.RefreshToken))
                await _authApi.LogoutAsync(new RefreshTokenRequest(Session.RefreshToken));
        }
        catch
        {
            // Best-effort server-side revocation — the local session is cleared either way.
        }
        finally
        {
            Session.Clear();
            LoggedOut?.Invoke();
        }
    }
}
