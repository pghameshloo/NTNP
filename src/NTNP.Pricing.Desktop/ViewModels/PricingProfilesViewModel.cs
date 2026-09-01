using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NTNP.Pricing.Contracts.PricingProfiles;
using NTNP.Pricing.Desktop.Services.Api;

namespace NTNP.Pricing.Desktop.ViewModels;

/// <summary>Screen 10 (Section 22/12) — Pricing Profiles.</summary>
public sealed partial class PricingProfilesViewModel : ViewModelBase
{
    private readonly PricingProfilesApiClient _api;

    public static readonly IReadOnlyList<string> PricingMethods = new[] { "Markup", "GrossMargin" };
    public static readonly IReadOnlyList<string> RoundingModes = new[] { "None", "NearestInteger", "NearestTen", "NearestHundred", "NearestThousand" };

    [ObservableProperty] private bool _includeInactive;
    [ObservableProperty] private PricingProfileDto? _selectedProfile;
    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private bool _isNew;

    [ObservableProperty] private string _formName = string.Empty;
    [ObservableProperty] private string _formPricingMethod = "Markup";
    [ObservableProperty] private decimal _formDefaultRate = 0.3m;
    [ObservableProperty] private decimal _formDefaultRialShare = 0.15m;
    [ObservableProperty] private decimal _formDefaultForeignShare = 0.85m;
    [ObservableProperty] private string _formDefaultQuotationCurrencyCode = "EUR";
    [ObservableProperty] private string _formIrrRoundingPolicy = "NearestThousand";
    [ObservableProperty] private string _formForeignRoundingPolicy = "NearestInteger";
    [ObservableProperty] private int _formForeignDecimalPlaces = 2;
    [ObservableProperty] private decimal _formReconciliationToleranceIrr = 1m;

    public ObservableCollection<PricingProfileDto> Profiles { get; } = new();

    public PricingProfilesViewModel(PricingProfilesApiClient api) => _api = api;

    public override Task OnNavigatedToAsync() => LoadAsync();

    [RelayCommand]
    private async Task LoadAsync() => await RunBusyAsync(async () =>
    {
        var list = await _api.ListAsync(IncludeInactive);
        Profiles.Clear();
        foreach (var p in list) Profiles.Add(p);
    });

    partial void OnSelectedProfileChanged(PricingProfileDto? value)
    {
        if (value is null) return;
        IsNew = false;
        IsEditing = true;
        FormName = value.Name;
        FormPricingMethod = value.PricingMethod;
        FormDefaultRate = value.DefaultRate;
        FormDefaultRialShare = value.DefaultRialShare;
        FormDefaultForeignShare = value.DefaultForeignShare;
        FormDefaultQuotationCurrencyCode = value.DefaultQuotationCurrencyCode;
        FormIrrRoundingPolicy = value.IrrRoundingPolicy;
        FormForeignRoundingPolicy = value.ForeignRoundingPolicy;
        FormForeignDecimalPlaces = value.ForeignDecimalPlaces;
        FormReconciliationToleranceIrr = value.ReconciliationToleranceIrr;
    }

    [RelayCommand]
    private void New()
    {
        SelectedProfile = null;
        IsNew = true;
        IsEditing = true;
        FormName = string.Empty;
        FormPricingMethod = "Markup";
        FormDefaultRate = 0.3m;
        FormDefaultRialShare = 0.15m;
        FormDefaultForeignShare = 0.85m;
        FormDefaultQuotationCurrencyCode = "EUR";
        FormIrrRoundingPolicy = "NearestThousand";
        FormForeignRoundingPolicy = "NearestInteger";
        FormForeignDecimalPlaces = 2;
        FormReconciliationToleranceIrr = 1m;
    }

    [RelayCommand] private void CancelEdit() => IsEditing = false;

    [RelayCommand]
    private async Task SaveAsync() => await RunBusyAsync(async () =>
    {
        var request = new UpsertPricingProfileRequest(
            FormName, FormPricingMethod, FormDefaultRate, FormDefaultRialShare, FormDefaultForeignShare,
            FormDefaultQuotationCurrencyCode, FormIrrRoundingPolicy, FormForeignRoundingPolicy, FormForeignDecimalPlaces,
            FormReconciliationToleranceIrr, IsNew ? null : SelectedProfile!.RowVersion);

        if (IsNew)
        {
            var created = await _api.CreateAsync(request);
            await LoadAsync();
            SelectedProfile = Profiles.FirstOrDefault(p => p.Id == created.Id);
        }
        else if (SelectedProfile is not null)
        {
            var updated = await _api.UpdateAsync(SelectedProfile.Id, request);
            var index = Profiles.IndexOf(SelectedProfile);
            if (index >= 0) Profiles[index] = updated;
            SelectedProfile = updated;
        }
        IsEditing = false;
    });
}
