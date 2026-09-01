using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NTNP.Pricing.Contracts.Auth;
using NTNP.Pricing.Desktop.Services;
using NTNP.Pricing.Desktop.Services.Api;

namespace NTNP.Pricing.Desktop.ViewModels;

/// <summary>Screen 1 (Section 22) — Login.</summary>
public sealed partial class LoginViewModel : ViewModelBase
{
    private readonly AuthApiClient _authApi;
    private readonly HealthApiClient _healthApi;
    private readonly AppSession _session;
    private readonly IServerConnectionSettingsService _serverSettings;

    [ObservableProperty] private string _userNameOrEmail = string.Empty;
    [ObservableProperty] private string _serverUrl;
    [ObservableProperty] private bool _rememberMe = true;
    [ObservableProperty] private string? _connectionStatus;
    [ObservableProperty] private bool _connectionOk;

    /// <summary>Raised once <see cref="AppSession.ApplyLogin"/> has been called — the view's code-behind closes the window on this.</summary>
    public event Action? LoginSucceeded;

    public LoginViewModel(AuthApiClient authApi, HealthApiClient healthApi, AppSession session, IServerConnectionSettingsService serverSettings)
    {
        _authApi = authApi;
        _healthApi = healthApi;
        _session = session;
        _serverSettings = serverSettings;
        _serverUrl = serverSettings.ApiBaseUrl;
    }

    public override async Task OnNavigatedToAsync()
    {
        await TestConnectionAsync();

        var remembered = _session.TryLoadRememberedRefreshToken();
        if (remembered is not null)
        {
            await RunBusyAsync(async () =>
            {
                var response = await _authApi.RefreshAsync(new RefreshTokenRequest(remembered));
                _session.ApplyLogin(response, remember: true);
                LoginSucceeded?.Invoke();
            });
        }
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        await _serverSettings.SetApiBaseUrlAsync(ServerUrl);
        try
        {
            var status = await _healthApi.GetStatusAsync();
            ConnectionOk = status.DatabaseReachable;
            ConnectionStatus = status.DatabaseReachable
                ? $"متصل — نسخه {status.ApiVersion}"
                : "سرور در دسترس است اما پایگاه‌داده در دسترس نیست.";
        }
        catch
        {
            ConnectionOk = false;
            ConnectionStatus = "امکان اتصال به سرور وجود ندارد.";
        }
    }

    public async Task LoginAsync(string password)
    {
        await RunBusyAsync(async () =>
        {
            await _serverSettings.SetApiBaseUrlAsync(ServerUrl);
            var response = await _authApi.LoginAsync(new LoginRequest(UserNameOrEmail, password));
            _session.ApplyLogin(response, RememberMe);
            LoginSucceeded?.Invoke();
        });
    }
}
