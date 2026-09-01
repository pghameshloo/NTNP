using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using NTNP.Pricing.Contracts.Auth;

namespace NTNP.Pricing.Desktop.Services;

/// <summary>
/// Section 6/7 — the desktop client's single source of truth for "who is signed in right now" and
/// the current access/refresh token pair. Nothing about calculation authority lives here — this is
/// purely a client-side identity/session cache; every actual authorization decision is re-checked
/// server-side on every request (Section 6: "hiding buttons is not authorization").
/// </summary>
public sealed partial class AppSession : ObservableObject
{
    private static readonly string RememberedTokenPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NTNP", "Pricing", "session.dat");

    [ObservableProperty] private UserDto? _currentUser;
    [ObservableProperty] private string? _accessToken;
    [ObservableProperty] private DateTimeOffset _accessTokenExpiresAtUtc;
    [ObservableProperty] private string? _refreshToken;
    [ObservableProperty] private bool _isServerReachable = true;

    public bool IsAuthenticated => CurrentUser is not null && !string.IsNullOrEmpty(AccessToken);

    public bool IsInRole(string role) => CurrentUser?.Roles.Contains(role) ?? false;

    public void ApplyLogin(LoginResponse response, bool remember)
    {
        CurrentUser = response.User;
        AccessToken = response.AccessToken;
        AccessTokenExpiresAtUtc = response.AccessTokenExpiresAtUtc;
        RefreshToken = response.RefreshToken;
        OnPropertyChanged(nameof(IsAuthenticated));

        if (remember)
            TrySaveRememberedToken(response.RefreshToken);
        else
            TryDeleteRememberedToken();
    }

    public void Clear()
    {
        CurrentUser = null;
        AccessToken = null;
        RefreshToken = null;
        OnPropertyChanged(nameof(IsAuthenticated));
        TryDeleteRememberedToken();
    }

    /// <summary>
    /// Section 7 (ASSUMPTIONS.md) — a "remember me" refresh token is persisted encrypted at rest via
    /// Windows DPAPI (CurrentUser scope: readable only by the same Windows account on the same
    /// machine), never in plaintext. Only the refresh token is stored — never the short-lived access
    /// token — so a stolen settings file is useless without also compromising the Windows user
    /// account, and a revoked/rotated token on the server still blocks reuse either way.
    /// </summary>
    public string? TryLoadRememberedRefreshToken()
    {
        try
        {
            if (!File.Exists(RememberedTokenPath)) return null;
            var protectedBytes = File.ReadAllBytes(RememberedTokenPath);
            var bytes = ProtectedData.Unprotect(protectedBytes, optionalEntropy: null, DataProtectionScope.CurrentUser);
            return JsonSerializer.Deserialize<string>(bytes);
        }
        catch
        {
            return null; // corrupt/foreign-machine file — treat as "not remembered", never crash startup
        }
    }

    private static void TrySaveRememberedToken(string refreshToken)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(RememberedTokenPath)!);
            var bytes = JsonSerializer.SerializeToUtf8Bytes(refreshToken);
            var protectedBytes = ProtectedData.Protect(bytes, optionalEntropy: null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(RememberedTokenPath, protectedBytes);
        }
        catch
        {
            // Non-fatal — the user simply won't be remembered next launch.
        }
    }

    private static void TryDeleteRememberedToken()
    {
        try
        {
            if (File.Exists(RememberedTokenPath)) File.Delete(RememberedTokenPath);
        }
        catch
        {
            // ignore
        }
    }
}
