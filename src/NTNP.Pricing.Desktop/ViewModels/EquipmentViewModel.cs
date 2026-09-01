using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NTNP.Pricing.Contracts.Equipment;
using NTNP.Pricing.Desktop.Services;
using NTNP.Pricing.Desktop.Services.Api;

namespace NTNP.Pricing.Desktop.ViewModels;

/// <summary>
/// Screens 4 and 5 (Section 22/9) — Equipment Database and, via the selected row's price-history
/// panel, Equipment Price History. Also hosts the Section 9 Excel import workflow (10-step
/// preview→commit) reached from the "درون‌ریزی از اکسل" button.
/// </summary>
public sealed partial class EquipmentViewModel : ViewModelBase
{
    private readonly EquipmentApiClient _api;
    private readonly IDialogService _dialogs;

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private bool _includeInactive;
    [ObservableProperty] private bool _missingPriceOnly;
    [ObservableProperty] private EquipmentDto? _selectedEquipment;
    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private bool _isNew;

    [ObservableProperty] private string _formCode = string.Empty;
    [ObservableProperty] private string? _formTechnicalPartNumber;
    [ObservableProperty] private string _formDescriptionFa = string.Empty;
    [ObservableProperty] private string _formDescriptionEn = string.Empty;
    [ObservableProperty] private string? _formCategory;
    [ObservableProperty] private string? _formSubcategory;
    [ObservableProperty] private string? _formBrand;
    [ObservableProperty] private string? _formModel;
    [ObservableProperty] private string? _formManufacturer;
    [ObservableProperty] private string? _formSupplier;
    [ObservableProperty] private string _formUnit = "EA";
    [ObservableProperty] private int? _formLeadTimeDays;
    [ObservableProperty] private string? _formNotes;
    [ObservableProperty] private bool _formIsActive = true;

    // New-price sub-form
    [ObservableProperty] private string _newPriceCurrencyCode = "IRR";
    [ObservableProperty] private decimal? _newPriceForeignUnitPrice;
    [ObservableProperty] private decimal? _newPriceRialUnitPrice;
    [ObservableProperty] private DateTimeOffset _newPriceEffectiveAt = DateTimeOffset.Now;
    [ObservableProperty] private string? _newPriceSourceText;

    public ObservableCollection<EquipmentDto> Equipment { get; } = new();
    public ObservableCollection<EquipmentPriceDto> PriceHistory { get; } = new();

    public EquipmentViewModel(EquipmentApiClient api, IDialogService dialogs)
    {
        _api = api;
        _dialogs = dialogs;
    }

    public override Task OnNavigatedToAsync() => SearchAsync();

    [RelayCommand]
    private async Task SearchAsync() => await RunBusyAsync(async () =>
    {
        var page = await _api.SearchAsync(SearchText, 1, 300, IncludeInactive, category: null, MissingPriceOnly);
        Equipment.Clear();
        foreach (var e in page.Items) Equipment.Add(e);
    });

    partial void OnSelectedEquipmentChanged(EquipmentDto? value)
    {
        PriceHistory.Clear();
        if (value is null) return;
        IsNew = false;
        IsEditing = true;
        FormCode = value.Code;
        FormTechnicalPartNumber = value.TechnicalPartNumber;
        FormDescriptionFa = value.DescriptionFa;
        FormDescriptionEn = value.DescriptionEn;
        FormCategory = value.Category;
        FormSubcategory = value.Subcategory;
        FormBrand = value.Brand;
        FormModel = value.Model;
        FormManufacturer = value.Manufacturer;
        FormSupplier = value.Supplier;
        FormUnit = value.Unit;
        FormLeadTimeDays = value.LeadTimeDays;
        FormNotes = value.Notes;
        FormIsActive = value.IsActive;
        _ = LoadPriceHistoryAsync(value.Id);
    }

    private async Task LoadPriceHistoryAsync(Guid equipmentId) => await RunBusyAsync(async () =>
    {
        var prices = await _api.GetPriceHistoryAsync(equipmentId);
        PriceHistory.Clear();
        foreach (var p in prices) PriceHistory.Add(p);
    });

    [RelayCommand]
    private void New()
    {
        SelectedEquipment = null;
        IsNew = true;
        IsEditing = true;
        FormCode = string.Empty;
        FormTechnicalPartNumber = FormCategory = FormSubcategory = FormBrand = FormModel = FormManufacturer = FormSupplier = FormNotes = null;
        FormDescriptionFa = FormDescriptionEn = string.Empty;
        FormUnit = "EA";
        FormLeadTimeDays = null;
        FormIsActive = true;
    }

    [RelayCommand] private void CancelEdit() => IsEditing = false;

    [RelayCommand]
    private async Task SaveAsync() => await RunBusyAsync(async () =>
    {
        if (IsNew)
        {
            var created = await _api.CreateAsync(new CreateEquipmentRequest(
                FormCode, FormTechnicalPartNumber, FormDescriptionFa, FormDescriptionEn, FormCategory, FormSubcategory,
                FormBrand, FormModel, FormManufacturer, FormSupplier, FormUnit, FormLeadTimeDays, FormNotes));
            await SearchAsync();
            SelectedEquipment = Equipment.FirstOrDefault(e => e.Id == created.Id);
        }
        else if (SelectedEquipment is not null)
        {
            var updated = await _api.UpdateAsync(SelectedEquipment.Id, new UpdateEquipmentRequest(
                FormTechnicalPartNumber, FormDescriptionFa, FormDescriptionEn, FormCategory, FormSubcategory,
                FormBrand, FormModel, FormManufacturer, FormSupplier, FormUnit, FormLeadTimeDays, FormNotes, FormIsActive, SelectedEquipment.RowVersion));
            var index = Equipment.IndexOf(SelectedEquipment);
            if (index >= 0) Equipment[index] = updated;
            SelectedEquipment = updated;
        }
        IsEditing = false;
    });

    [RelayCommand]
    private async Task AddPriceAsync() => await RunBusyAsync(async () =>
    {
        if (SelectedEquipment is null) return;
        await _api.AddPriceAsync(new CreateEquipmentPriceRequest(
            SelectedEquipment.Id, NewPriceCurrencyCode, NewPriceForeignUnitPrice, NewPriceRialUnitPrice, NewPriceEffectiveAt, NewPriceSourceText, null));
        await LoadPriceHistoryAsync(SelectedEquipment.Id);

        // The equipment row's CurrentPrice/HasMissingPrice flags may have changed — refresh it from the grid data too.
        var refreshed = await _api.GetAsync(SelectedEquipment.Id);
        var index = Equipment.IndexOf(SelectedEquipment);
        if (index >= 0) Equipment[index] = refreshed;
        SelectedEquipment = refreshed;

        NewPriceForeignUnitPrice = null;
        NewPriceRialUnitPrice = null;
        NewPriceSourceText = null;
    });

    /// <summary>Section 9's 10-step Excel import: pick a file, preview insert/update/error counts, confirm, commit.</summary>
    [RelayCommand]
    private async Task ImportFromExcelAsync() => await RunBusyAsync(async () =>
    {
        var path = _dialogs.ShowOpenFileDialog("Excel Workbook (*.xlsx)|*.xlsx");
        if (path is null) return;

        var bytes = await File.ReadAllBytesAsync(path);
        var preview = await _api.PreviewImportAsync(bytes, Path.GetFileName(path));

        var summary = $"ردیف‌های جدید: {preview.InsertCount}{Environment.NewLine}" +
                      $"ردیف‌های بروزرسانی: {preview.UpdateCount}{Environment.NewLine}" +
                      $"ردیف‌های خطادار: {preview.ErrorCount}{Environment.NewLine}{Environment.NewLine}" +
                      "آیا از اعمال این درون‌ریزی اطمینان دارید؟";

        if (preview.ErrorCount > 0)
        {
            var errorLines = preview.Rows.Where(r => r.Errors.Count > 0)
                .Take(20)
                .Select(r => $"ردیف {r.RowNumber}: {string.Join("، ", r.Errors)}");
            _dialogs.ShowError("خطاهای درون‌ریزی", string.Join(Environment.NewLine, errorLines));
        }

        if (preview.InsertCount + preview.UpdateCount == 0) return;
        if (!_dialogs.Confirm("تأیید درون‌ریزی از اکسل", summary)) return;

        var result = await _api.CommitImportAsync(new EquipmentImportCommitRequest(preview.ImportToken));
        _dialogs.ShowInfo("درون‌ریزی کامل شد", $"{result.InsertedCount} ردیف جدید و {result.UpdatedCount} ردیف بروزرسانی شد.");
        await SearchAsync();
    });
}
