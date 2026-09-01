using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NTNP.Pricing.Contracts.Currencies;
using NTNP.Pricing.Desktop.Services.Api;

namespace NTNP.Pricing.Desktop.ViewModels;

/// <summary>Screen 6 (Section 22/8) — Currencies and Exchange Rates.</summary>
public sealed partial class CurrenciesViewModel : ViewModelBase
{
    private readonly CurrenciesApiClient _api;

    [ObservableProperty] private bool _includeInactive;
    [ObservableProperty] private CurrencyDto? _selectedCurrency;

    [ObservableProperty] private string _newCurrencyCode = string.Empty;
    [ObservableProperty] private string _newCurrencyName = string.Empty;
    [ObservableProperty] private string _newCurrencySymbol = string.Empty;
    [ObservableProperty] private bool _showAddCurrency;

    [ObservableProperty] private decimal? _newRatePurchase;
    [ObservableProperty] private decimal? _newRateSelling;
    [ObservableProperty] private DateTimeOffset _newRateEffectiveAt = DateTimeOffset.Now;
    [ObservableProperty] private string? _newRateSource;
    [ObservableProperty] private string? _newRateNotes;

    public ObservableCollection<CurrencyDto> Currencies { get; } = new();
    public ObservableCollection<ExchangeRateDto> RateHistory { get; } = new();

    public CurrenciesViewModel(CurrenciesApiClient api) => _api = api;

    public override Task OnNavigatedToAsync() => LoadAsync();

    [RelayCommand]
    private async Task LoadAsync() => await RunBusyAsync(LoadCoreAsync);

    // Unwrapped core so AddRateAsync (already inside a RunBusyAsync scope) can refresh the master grid
    // without tripping RunBusyAsync's "already busy" reentrancy guard.
    private async Task LoadCoreAsync()
    {
        var list = await _api.ListAsync(IncludeInactive);
        Currencies.Clear();
        foreach (var c in list) Currencies.Add(c);
    }

    partial void OnSelectedCurrencyChanged(CurrencyDto? value)
    {
        RateHistory.Clear();
        if (value is null) return;
        ShowAddCurrency = false;
        _ = LoadRateHistoryAsync(value.Id);
    }

    private async Task LoadRateHistoryAsync(Guid currencyId) => await RunBusyAsync(async () =>
    {
        var rates = await _api.GetRateHistoryAsync(currencyId);
        RateHistory.Clear();
        foreach (var r in rates) RateHistory.Add(r);
    });

    [RelayCommand]
    private void ShowAddCurrencyForm()
    {
        SelectedCurrency = null; // the two right-pane panels (add-currency / rate-history) are mutually exclusive
        ShowAddCurrency = true;
    }
    [RelayCommand] private void CancelAddCurrency() => ShowAddCurrency = false;

    [RelayCommand]
    private async Task AddCurrencyAsync() => await RunBusyAsync(async () =>
    {
        var isBase = Currencies.Count == 0; // the first currency registered is conventionally IRR, the base currency
        var created = await _api.CreateAsync(new CreateCurrencyRequest(NewCurrencyCode, NewCurrencyName, NewCurrencySymbol, isBase));
        Currencies.Add(created);
        NewCurrencyCode = NewCurrencyName = NewCurrencySymbol = string.Empty;
        ShowAddCurrency = false;
        SelectedCurrency = created;
    });

    [RelayCommand]
    private async Task AddRateAsync() => await RunBusyAsync(async () =>
    {
        if (SelectedCurrency is null || NewRatePurchase is null || NewRateSelling is null) return;
        var rate = await _api.AddRateAsync(new CreateExchangeRateRequest(SelectedCurrency.Id, NewRatePurchase.Value, NewRateSelling.Value, NewRateEffectiveAt, NewRateSource, NewRateNotes));
        RateHistory.Insert(0, rate);
        NewRatePurchase = NewRateSelling = null;
        NewRateSource = NewRateNotes = null;
        await LoadCoreAsync(); // refresh each currency's LatestRate in the master grid
        SelectedCurrency = Currencies.FirstOrDefault(c => c.Id == rate.CurrencyId);
    });
}
