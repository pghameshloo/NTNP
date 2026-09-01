using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NTNP.Pricing.Contracts.Customers;
using NTNP.Pricing.Desktop.Services;
using NTNP.Pricing.Desktop.Services.Api;

namespace NTNP.Pricing.Desktop.ViewModels;

/// <summary>Screen 3 (Section 22/7) — Customers master-data: search grid + master/detail editor.</summary>
public sealed partial class CustomersViewModel : ViewModelBase
{
    private readonly CustomersApiClient _api;
    private readonly IDialogService _dialogs;

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private bool _includeInactive;
    [ObservableProperty] private CustomerDto? _selectedCustomer;
    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private bool _isNew;

    // Editable form fields — kept separate from CustomerDto (an immutable record) so the grid's
    // selection isn't mutated in place while the user is still typing.
    [ObservableProperty] private string _formCustomerCode = string.Empty;
    [ObservableProperty] private string _formCompanyName = string.Empty;
    [ObservableProperty] private string? _formIndustry;
    [ObservableProperty] private string? _formRegistrationNumber;
    [ObservableProperty] private string? _formTaxId;
    [ObservableProperty] private string? _formContactPerson;
    [ObservableProperty] private string? _formContactPosition;
    [ObservableProperty] private string? _formPhone;
    [ObservableProperty] private string? _formEmail;
    [ObservableProperty] private string? _formAddress;
    [ObservableProperty] private string? _formNotes;
    [ObservableProperty] private bool _formIsActive = true;

    public ObservableCollection<CustomerDto> Customers { get; } = new();

    public CustomersViewModel(CustomersApiClient api, IDialogService dialogs)
    {
        _api = api;
        _dialogs = dialogs;
    }

    public override Task OnNavigatedToAsync() => SearchAsync();

    [RelayCommand]
    private async Task SearchAsync() => await RunBusyAsync(SearchCoreAsync);

    // Unwrapped core so internal callers already inside a RunBusyAsync scope (e.g. SaveAsync's create
    // path, which needs to reload the grid after creating a row) can reuse it without tripping
    // RunBusyAsync's "already busy" guard — that guard would otherwise silently no-op a nested call.
    private async Task SearchCoreAsync()
    {
        var page = await _api.SearchAsync(SearchText, 1, 200, IncludeInactive);
        Customers.Clear();
        foreach (var c in page.Items) Customers.Add(c);
    }

    partial void OnSelectedCustomerChanged(CustomerDto? value)
    {
        if (value is null) return;
        IsNew = false;
        IsEditing = true;
        FormCustomerCode = value.CustomerCode;
        FormCompanyName = value.CompanyName;
        FormIndustry = value.Industry;
        FormRegistrationNumber = value.RegistrationNumber;
        FormTaxId = value.TaxId;
        FormContactPerson = value.ContactPerson;
        FormContactPosition = value.ContactPosition;
        FormPhone = value.Phone;
        FormEmail = value.Email;
        FormAddress = value.Address;
        FormNotes = value.Notes;
        FormIsActive = value.IsActive;
    }

    [RelayCommand]
    private void New()
    {
        SelectedCustomer = null;
        IsNew = true;
        IsEditing = true;
        FormCustomerCode = string.Empty;
        FormCompanyName = string.Empty;
        FormIndustry = FormRegistrationNumber = FormTaxId = FormContactPerson = FormContactPosition = FormPhone = FormEmail = FormAddress = FormNotes = null;
        FormIsActive = true;
    }

    [RelayCommand] private void CancelEdit() => IsEditing = false;

    [RelayCommand]
    private async Task SaveAsync() => await RunBusyAsync(async () =>
    {
        if (IsNew)
        {
            var created = await _api.CreateAsync(new CreateCustomerRequest(
                FormCustomerCode, FormCompanyName, FormIndustry, FormRegistrationNumber, FormTaxId,
                FormContactPerson, FormContactPosition, FormPhone, FormEmail, FormAddress, FormNotes));
            await SearchCoreAsync();
            SelectedCustomer = Customers.FirstOrDefault(c => c.Id == created.Id);
        }
        else if (SelectedCustomer is not null)
        {
            var updated = await _api.UpdateAsync(SelectedCustomer.Id, new UpdateCustomerRequest(
                FormCompanyName, FormIndustry, FormRegistrationNumber, FormTaxId, FormContactPerson, FormContactPosition,
                FormPhone, FormEmail, FormAddress, FormNotes, FormIsActive, SelectedCustomer.RowVersion));
            var index = Customers.IndexOf(SelectedCustomer);
            if (index >= 0) Customers[index] = updated;
            SelectedCustomer = updated;
        }
        IsEditing = false;
    });
}
