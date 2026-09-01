using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NTNP.Pricing.Contracts.Settings;
using NTNP.Pricing.Desktop.Services;
using NTNP.Pricing.Desktop.Services.Api;

namespace NTNP.Pricing.Desktop.ViewModels;

/// <summary>
/// Screens 23 and 24 (Section 22/23/26/33) — Company and System Settings, and Report Template
/// Settings. Both are exposed as tabs over the one <see cref="CompanySettingsDto"/> the server
/// stores (branding/contact fields on one tab, quotation defaults + signature block on the other) —
/// there is no separate "report template" record on the server to point a second screen at.
/// </summary>
public sealed partial class CompanySettingsViewModel : ViewModelBase
{
    private readonly SettingsApiClient _api;
    private readonly IDialogService _dialogs;

    [ObservableProperty] private string _legalNameEn = string.Empty;
    [ObservableProperty] private string _legalNameFa = string.Empty;
    [ObservableProperty] private string? _address;
    [ObservableProperty] private string? _phone;
    [ObservableProperty] private string? _email;
    [ObservableProperty] private string? _website;
    [ObservableProperty] private string? _logoStoragePath;
    [ObservableProperty] private string? _stampImageStoragePath;

    [ObservableProperty] private string _defaultQuotationTitleFa = string.Empty;
    [ObservableProperty] private string _defaultQuotationTitleEn = string.Empty;
    [ObservableProperty] private string _confidentialityLabelFa = string.Empty;
    [ObservableProperty] private string _confidentialityLabelEn = string.Empty;
    [ObservableProperty] private string? _defaultDeliveryTerms;
    [ObservableProperty] private string? _defaultPaymentTerms;
    [ObservableProperty] private string? _defaultWarrantyTerms;
    [ObservableProperty] private string? _defaultInspectionTerms;
    [ObservableProperty] private string? _defaultPackingTerms;
    [ObservableProperty] private string? _defaultTransportationTerms;
    [ObservableProperty] private string? _defaultTaxesAndDutiesNote;
    [ObservableProperty] private string? _defaultScopeExclusions;
    [ObservableProperty] private string _preparedByName = string.Empty;
    [ObservableProperty] private string _preparedByPosition = string.Empty;
    [ObservableProperty] private string _commercialManagerName = string.Empty;
    [ObservableProperty] private string _commercialManagerPosition = string.Empty;
    [ObservableProperty] private string _managingDirectorName = string.Empty;
    [ObservableProperty] private string _managingDirectorPosition = string.Empty;
    [ObservableProperty] private bool _enableCustomerAcceptanceBlock;
    [ObservableProperty] private int _staleExchangeRateDays = 180;

    public CompanySettingsViewModel(SettingsApiClient api, IDialogService dialogs)
    {
        _api = api;
        _dialogs = dialogs;
    }

    public override Task OnNavigatedToAsync() => LoadAsync();

    [RelayCommand]
    private async Task LoadAsync() => await RunBusyAsync(async () =>
    {
        var s = await _api.GetAsync();
        Apply(s);
    });

    private void Apply(CompanySettingsDto s)
    {
        LegalNameEn = s.LegalNameEn;
        LegalNameFa = s.LegalNameFa;
        Address = s.Address;
        Phone = s.Phone;
        Email = s.Email;
        Website = s.Website;
        LogoStoragePath = s.LogoStoragePath;
        StampImageStoragePath = s.StampImageStoragePath;
        DefaultQuotationTitleFa = s.DefaultQuotationTitleFa;
        DefaultQuotationTitleEn = s.DefaultQuotationTitleEn;
        ConfidentialityLabelFa = s.ConfidentialityLabelFa;
        ConfidentialityLabelEn = s.ConfidentialityLabelEn;
        DefaultDeliveryTerms = s.DefaultDeliveryTerms;
        DefaultPaymentTerms = s.DefaultPaymentTerms;
        DefaultWarrantyTerms = s.DefaultWarrantyTerms;
        DefaultInspectionTerms = s.DefaultInspectionTerms;
        DefaultPackingTerms = s.DefaultPackingTerms;
        DefaultTransportationTerms = s.DefaultTransportationTerms;
        DefaultTaxesAndDutiesNote = s.DefaultTaxesAndDutiesNote;
        DefaultScopeExclusions = s.DefaultScopeExclusions;
        PreparedByName = s.PreparedByName;
        PreparedByPosition = s.PreparedByPosition;
        CommercialManagerName = s.CommercialManagerName;
        CommercialManagerPosition = s.CommercialManagerPosition;
        ManagingDirectorName = s.ManagingDirectorName;
        ManagingDirectorPosition = s.ManagingDirectorPosition;
        EnableCustomerAcceptanceBlock = s.EnableCustomerAcceptanceBlock;
        StaleExchangeRateDays = s.StaleExchangeRateDays;
    }

    [RelayCommand]
    private async Task SaveAsync() => await RunBusyAsync(async () =>
    {
        var dto = new CompanySettingsDto(
            LegalNameEn, LegalNameFa, Address, Phone, Email, Website, LogoStoragePath, StampImageStoragePath,
            DefaultQuotationTitleFa, DefaultQuotationTitleEn, ConfidentialityLabelFa, ConfidentialityLabelEn,
            DefaultDeliveryTerms, DefaultPaymentTerms, DefaultWarrantyTerms, DefaultInspectionTerms, DefaultPackingTerms,
            DefaultTransportationTerms, DefaultTaxesAndDutiesNote, DefaultScopeExclusions,
            PreparedByName, PreparedByPosition, CommercialManagerName, CommercialManagerPosition,
            ManagingDirectorName, ManagingDirectorPosition, EnableCustomerAcceptanceBlock, StaleExchangeRateDays);

        var saved = await _api.UpdateAsync(new UpdateCompanySettingsRequest(dto));
        Apply(saved);
        _dialogs.ShowInfo("ذخیره شد", "تنظیمات شرکت با موفقیت ذخیره شد.");
    });
}
