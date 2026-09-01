using NTNP.Pricing.Desktop.Services;

namespace NTNP.Pricing.Desktop.Tests.TestSupport;

public sealed class FakeServerConnectionSettingsService : IServerConnectionSettingsService
{
    public string ApiBaseUrl { get; private set; } = "http://fake-server.test";

    public Task SetApiBaseUrlAsync(string apiBaseUrl, CancellationToken ct = default)
    {
        ApiBaseUrl = apiBaseUrl;
        return Task.CompletedTask;
    }
}

/// <summary>Records every call instead of showing real UI, and answers Confirm with a scriptable result.</summary>
public sealed class FakeDialogService : IDialogService
{
    public bool ConfirmResult { get; set; } = true;
    public string? SaveFileDialogResult { get; set; }
    public string? OpenFileDialogResult { get; set; }

    public List<(string Title, string Message)> Errors { get; } = new();
    public List<(string Title, string Message)> InfoMessages { get; } = new();
    public List<(string Title, string Message)> Confirmations { get; } = new();

    public bool Confirm(string title, string message)
    {
        Confirmations.Add((title, message));
        return ConfirmResult;
    }

    public void ShowError(string title, string message) => Errors.Add((title, message));
    public void ShowInfo(string title, string message) => InfoMessages.Add((title, message));
    public string? ShowSaveFileDialog(string suggestedFileName, string filter) => SaveFileDialogResult;
    public string? ShowOpenFileDialog(string filter) => OpenFileDialogResult;
}
