using System.IO;
using System.Text.Json;

namespace NTNP.Pricing.Desktop.Services;

/// <summary>
/// Section 22/25 (Server Connection Settings screen), Section 33 — the desktop client never talks
/// to SQL Server directly and never hardcodes the API address; it is stored per-machine so the same
/// installed client can point at different environments (e.g. a pilot server, then production)
/// without a reinstall.
/// </summary>
public interface IServerConnectionSettingsService
{
    string ApiBaseUrl { get; }
    Task SetApiBaseUrlAsync(string apiBaseUrl, CancellationToken ct = default);
}

file sealed record PersistedSettings(string ApiBaseUrl);

public sealed class ServerConnectionSettingsService : IServerConnectionSettingsService
{
    private const string DefaultApiBaseUrl = "http://localhost:5252";
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NTNP", "Pricing", "client-settings.json");

    public string ApiBaseUrl { get; private set; }

    public ServerConnectionSettingsService()
    {
        ApiBaseUrl = LoadOrDefault();
    }

    private static string LoadOrDefault()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                var loaded = JsonSerializer.Deserialize<PersistedSettings>(json);
                if (!string.IsNullOrWhiteSpace(loaded?.ApiBaseUrl))
                    return loaded.ApiBaseUrl;
            }
        }
        catch
        {
            // A corrupt/unreadable settings file must never block the app from starting — fall
            // through to the machine-wide default (or the hardcoded one) instead.
        }

        return TryReadMachineDefault() ?? DefaultApiBaseUrl;
    }

    /// <summary>
    /// Section 34 — the Windows installer's "Server Address" setup page writes the administrator's
    /// chosen address to <c>HKLM\Software\NTNP\Pricing\ServerUrl</c> (see
    /// installer/NTNP.Pricing.Installer) so every user on a shared workstation gets the right
    /// default without having to know the address themselves; the per-user
    /// <see cref="SettingsPath"/> file (checked first, above) still lets an individual user override
    /// it — e.g. to point at a pilot/test server — without touching the machine-wide default.
    /// </summary>
    private static string? TryReadMachineDefault()
    {
        try
        {
            if (!OperatingSystem.IsWindows()) return null;
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"Software\NTNP\Pricing");
            var value = key?.GetValue("ServerUrl") as string;
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
        catch
        {
            return null;
        }
    }

    public async Task SetApiBaseUrlAsync(string apiBaseUrl, CancellationToken ct = default)
    {
        ApiBaseUrl = apiBaseUrl.TrimEnd('/');
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        await File.WriteAllTextAsync(SettingsPath, JsonSerializer.Serialize(new PersistedSettings(ApiBaseUrl)), ct);
    }
}
