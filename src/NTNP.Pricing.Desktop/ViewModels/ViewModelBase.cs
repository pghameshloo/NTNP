using CommunityToolkit.Mvvm.ComponentModel;
using NTNP.Pricing.Desktop.Services;

namespace NTNP.Pricing.Desktop.ViewModels;

/// <summary>
/// Common plumbing every screen view model shares: a busy flag DataGrids/buttons bind against to
/// show the Section 23 "loading" state, a last-error surface for the Section 23 "error" state, and a
/// helper that wraps an API call so every view model handles <see cref="ApiException"/> the same way
/// instead of each screen reinventing try/catch.
/// </summary>
public abstract partial class ViewModelBase : ObservableObject
{
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _errorMessage;

    /// <summary>Called by <see cref="Services.INavigationService"/> right after the view model becomes current — the natural place to kick off an initial load.</summary>
    public virtual Task OnNavigatedToAsync() => Task.CompletedTask;

    protected async Task RunBusyAsync(Func<Task> action)
    {
        if (IsBusy) return;
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            await action();
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "سرور در دسترس نیست. اتصال شبکه یا تنظیمات سرور را بررسی کنید.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    protected async Task<T?> RunBusyAsync<T>(Func<Task<T>> action)
    {
        if (IsBusy) return default;
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            return await action();
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.Message;
            return default;
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "سرور در دسترس نیست. اتصال شبکه یا تنظیمات سرور را بررسی کنید.";
            return default;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
