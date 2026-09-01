using Microsoft.Win32;
using System.Windows;

namespace NTNP.Pricing.Desktop.Services;

/// <summary>Confirmation/error/save-file chrome shared by every screen, kept behind an interface so view models stay testable (Desktop.Tests uses a fake).</summary>
public interface IDialogService
{
    bool Confirm(string title, string message);
    void ShowError(string title, string message);
    void ShowInfo(string title, string message);

    /// <summary>Returns the chosen full path, or null if the user cancelled.</summary>
    string? ShowSaveFileDialog(string suggestedFileName, string filter);

    /// <summary>Returns the chosen full path, or null if the user cancelled.</summary>
    string? ShowOpenFileDialog(string filter);
}

public sealed class DialogService : IDialogService
{
    public bool Confirm(string title, string message) =>
        MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes;

    public void ShowError(string title, string message) => MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);

    public void ShowInfo(string title, string message) => MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);

    public string? ShowSaveFileDialog(string suggestedFileName, string filter)
    {
        var dialog = new SaveFileDialog { FileName = suggestedFileName, Filter = filter };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? ShowOpenFileDialog(string filter)
    {
        var dialog = new OpenFileDialog { Filter = filter };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
