using System.Windows;
using NTNP.Pricing.Desktop.ViewModels;

namespace NTNP.Pricing.Desktop.Views;

public partial class ShellWindow : Window
{
    /// <summary>Set just before <see cref="Window.Close"/> when the window is closing because of Logout, not a real application exit — see App.xaml.cs.</summary>
    public bool IsLoggingOut { get; private set; }

    public ShellWindow(ShellViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.LoggedOut += () => Dispatcher.Invoke(() =>
        {
            IsLoggingOut = true;
            Close();
        });
        WindowState = WindowState.Maximized;
        Loaded += async (_, _) => await viewModel.NavigateDashboardCommand.ExecuteAsync(null);
    }
}
