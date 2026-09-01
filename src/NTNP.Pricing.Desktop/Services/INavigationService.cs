using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using NTNP.Pricing.Desktop.ViewModels;

namespace NTNP.Pricing.Desktop.Services;

/// <summary>
/// View-model-first navigation for the shell's content area (Section 23: "current module" +
/// breadcrumb in the header, collapsible RTL nav on the side). The shell binds its
/// <c>ContentControl</c> to <see cref="CurrentViewModel"/>; a <c>DataTemplate</c> per view model
/// type (declared in <c>App.xaml</c>) supplies the matching view. Implements
/// <see cref="INotifyPropertyChanged"/> itself so a plain <c>{Binding Navigation.CurrentViewModel}</c>
/// in the shell's XAML updates on every navigation without the shell view model having to forward
/// each property by hand.
/// </summary>
public interface INavigationService : INotifyPropertyChanged
{
    ViewModelBase? CurrentViewModel { get; }
    string CurrentModuleTitle { get; }
    string? CurrentModuleGroup { get; }

    /// <summary>Navigates to a view model resolved fresh from DI (Section 23: every navigation reloads current server data).</summary>
    Task NavigateToAsync<TViewModel>(string moduleTitle, string? moduleGroup = null) where TViewModel : ViewModelBase;

    /// <summary>Navigates to an already-constructed view model instance (used when the caller needs to pass constructor/runtime state, e.g. "open this specific project").</summary>
    Task NavigateToAsync(ViewModelBase viewModel, string moduleTitle, string? moduleGroup = null);
}

public sealed class NavigationService : INavigationService
{
    private readonly IServiceProvider _services;

    public NavigationService(IServiceProvider services) => _services = services;

    private ViewModelBase? _currentViewModel;
    public ViewModelBase? CurrentViewModel
    {
        get => _currentViewModel;
        private set { _currentViewModel = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentViewModel))); }
    }

    private string _currentModuleTitle = string.Empty;
    public string CurrentModuleTitle
    {
        get => _currentModuleTitle;
        private set { _currentModuleTitle = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentModuleTitle))); }
    }

    private string? _currentModuleGroup;
    public string? CurrentModuleGroup
    {
        get => _currentModuleGroup;
        private set { _currentModuleGroup = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentModuleGroup))); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public Task NavigateToAsync<TViewModel>(string moduleTitle, string? moduleGroup = null) where TViewModel : ViewModelBase
    {
        var viewModel = _services.GetRequiredService<TViewModel>();
        return NavigateToAsync(viewModel, moduleTitle, moduleGroup);
    }

    public async Task NavigateToAsync(ViewModelBase viewModel, string moduleTitle, string? moduleGroup = null)
    {
        CurrentViewModel = viewModel;
        CurrentModuleTitle = moduleTitle;
        CurrentModuleGroup = moduleGroup;
        await viewModel.OnNavigatedToAsync();
    }
}
