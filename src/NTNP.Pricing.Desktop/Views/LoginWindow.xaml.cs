using System.Windows;
using System.Windows.Input;
using NTNP.Pricing.Desktop.ViewModels;

namespace NTNP.Pricing.Desktop.Views;

public partial class LoginWindow : Window
{
    private readonly LoginViewModel _viewModel;

    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        viewModel.LoginSucceeded += () => Dispatcher.Invoke(Close);
        Loaded += async (_, _) => await viewModel.OnNavigatedToAsync();
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e) => await _viewModel.LoginAsync(PasswordBox.Password);

    private async void PasswordBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            await _viewModel.LoginAsync(PasswordBox.Password);
    }
}
