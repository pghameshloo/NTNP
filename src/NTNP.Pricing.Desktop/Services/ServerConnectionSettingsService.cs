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
            if (!File.Exists(SettingsPath)) return DefaultApiBaseUrl;
            var json = File.ReadAllText(SettingsPath);
            var loaded = JsonSerializer.Deserialize<PersistedSettings>(json);
            return string.IsNullOrWhiteSpace(loaded?.ApiBaseUrl) ? DefaultApiBaseUrl : loaded.ApiBaseUrl;
        }
        catch
        {
            // A corrupt/unreadable settings file must never block the app from starting — fall back
            // to the default and let the user re-enter the server address on the Login/Settings screen.
            return DefaultApiBaseUrl;
        }
    }

    public async Task SetApiBaseUrlAsync(string apiBaseUrl, CancellationToken ct = default)
    {
        ApiBaseUrl = apiBaseUrl.TrimEnd('/');
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        await File.WriteAllTextAsync(SettingsPath, JsonSerializer.Serialize(new PersistedSettings(ApiBaseUrl)), ct);
    }
}
