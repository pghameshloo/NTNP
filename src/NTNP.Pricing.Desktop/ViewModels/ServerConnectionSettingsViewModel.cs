using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NTNP.Pricing.Desktop.Services;
using NTNP.Pricing.Desktop.Services.Api;

namespace NTNP.Pricing.Desktop.ViewModels;

/// <summary>Screen 25 (Section 22/33) — Server Connection Settings.</summary>
public sealed partial class ServerConnectionSettingsViewModel : ViewModelBase
{
    private readonly IServerConnectionSettingsService _settings;
    private readonly HealthApiClient _healthApi;

    [ObservableProperty] private string _apiBaseUrl;
    [ObservableProperty] private string? _connectionStatus;
    [ObservableProperty] private bool _connectionOk;
    [ObservableProperty] private string? _apiVersion;
    [ObservableProperty] private string? _databaseSchemaVersion;

    public ServerConnectionSettingsViewModel(IServerConnectionSettingsService settings, HealthApiClient healthApi)
    {
        _settings = settings;
        _healthApi = healthApi;
        _apiBaseUrl = settings.ApiBaseUrl;
    }

    public override Task OnNavigatedToAsync() => TestConnectionAsync();

    [RelayCommand]
    private async Task TestConnectionAsync() => await RunBusyAsync(async () =>
    {
        await _settings.SetApiBaseUrlAsync(ApiBaseUrl);
        try
        {
            var status = await _healthApi.GetStatusAsync();
            ConnectionOk = status.DatabaseReachable;
            ApiVersion = status.ApiVersion;
            DatabaseSchemaVersion = status.DatabaseSchemaVersion;
            ConnectionStatus = status.DatabaseReachable ? "اتصال به سرور و پایگاه‌داده برقرار است." : "سرور در دسترس است اما پایگاه‌داده در دسترس نیست.";
        }
        catch
        {
            ConnectionOk = false;
            ConnectionStatus = "امکان اتصال به سرور وجود ندارد. آدرس را بررسی کنید.";
        }
    });

    [RelayCommand]
    private async Task SaveAsync() => await RunBusyAsync(() => _settings.SetApiBaseUrlAsync(ApiBaseUrl));
}
