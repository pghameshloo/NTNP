using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NTNP.Pricing.Contracts.BodyEs;
using NTNP.Pricing.Contracts.Equipment;
using NTNP.Pricing.Contracts.PanelTemplates;
using NTNP.Pricing.Desktop.Services;
using NTNP.Pricing.Desktop.Services.Api;

namespace NTNP.Pricing.Desktop.ViewModels;

/// <summary>Screens 7 and 8 (Section 22/10) — Panel Templates and, as the selected template's BOM grid, the Panel BOM Editor.</summary>
public sealed partial class PanelTemplatesViewModel : ViewModelBase
{
    private readonly PanelTemplatesApiClient _api;
    private readonly LookupApiClient _lookups;
    private readonly EquipmentApiClient _equipmentApi;
    private readonly BodyEsTemplatesApiClient _bodyEsApi;
    private readonly IDialogService _dialogs;

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private PanelTemplateDto? _selectedTemplate;
    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private bool _isNew;

    [ObservableProperty] private string _formTemplateCode = string.Empty;
    [ObservableProperty] private string _formTemplateName = string.Empty;
    [ObservableProperty] private ProductFamilyDto? _formProductFamily;
    [ObservableProperty] private string? _formVoltageLevel;
    [ObservableProperty] private PanelTypeDto? _formPanelType;
    [ObservableProperty] private string? _formTechnicalDescription;
    [ObservableProperty] private BodyEsTemplateDto? _formBodyEsTemplate;
    [ObservableProperty] private string? _formNotes;

    [ObservableProperty] private EquipmentDto? _pickerSelectedEquipment;

    public ObservableCollection<PanelTemplateDto> Templates { get; } = new();
    public ObservableCollection<ProductFamilyDto> ProductFamilies { get; } = new();
    public ObservableCollection<PanelTypeDto> PanelTypes { get; } = new();
    public ObservableCollection<BodyEsTemplateDto> BodyEsTemplates { get; } = new();
    public ObservableCollection<EquipmentDto> EquipmentPickList { get; } = new();
    public ObservableCollection<PanelTemplateBomItemFormRow> BomItems { get; } = new();

    public PanelTemplatesViewModel(PanelTemplatesApiClient api, LookupApiClient lookups, EquipmentApiClient equipmentApi, BodyEsTemplatesApiClient bodyEsApi, IDialogService dialogs)
    {
        _api = api;
        _lookups = lookups;
        _equipmentApi = equipmentApi;
        _bodyEsApi = bodyEsApi;
        _dialogs = dialogs;
    }

    public override async Task OnNavigatedToAsync()
    {
        await RunBusyAsync(async () =>
        {
            var families = await _lookups.ProductFamiliesAsync();
            ProductFamilies.Clear();
            foreach (var f in families) ProductFamilies.Add(f);

            var types = await _lookups.PanelTypesAsync();
            PanelTypes.Clear();
            foreach (var t in types) PanelTypes.Add(t);

            var bodyEs = await _bodyEsApi.SearchAsync(null, 1, 200, null, null);
            BodyEsTemplates.Clear();
            foreach (var b in bodyEs.Items) BodyEsTemplates.Add(b);

            var equipmentPage = await _equipmentApi.SearchAsync(null, 1, 2000, false, null, false);
            EquipmentPickList.Clear();
            foreach (var e in equipmentPage.Items) EquipmentPickList.Add(e);
        });
        await SearchAsync();
    }

    [RelayCommand]
    private async Task SearchAsync() => await RunBusyAsync(SearchCoreAsync);

    // Unwrapped core so callers already inside a RunBusyAsync scope (SaveAsync, CreateNewRevisionAsync)
    // can reload the grid without tripping RunBusyAsync's "already busy" reentrancy guard.
    private async Task SearchCoreAsync()
    {
        var page = await _api.SearchAsync(SearchText, 1, 200, null, null);
        Templates.Clear();
        foreach (var t in page.Items) Templates.Add(t);
    }

    partial void OnSelectedTemplateChanged(PanelTemplateDto? value)
    {
        BomItems.Clear();
        if (value is null) return;
        IsNew = false;
        IsEditing = true;
        FormTemplateCode = value.TemplateCode;
        FormTemplateName = value.TemplateName;
        FormProductFamily = ProductFamilies.FirstOrDefault(f => f.Id == value.ProductFamilyId);
        FormVoltageLevel = value.VoltageLevel;
        FormPanelType = PanelTypes.FirstOrDefault(t => t.Id == value.PanelTypeId);
        FormTechnicalDescription = value.TechnicalDescription;
        FormBodyEsTemplate = value.BodyEsTemplateId is null ? null : BodyEsTemplates.FirstOrDefault(b => b.Id == value.BodyEsTemplateId);
        FormNotes = value.Notes;

        foreach (var item in value.BomItems.OrderBy(i => i.SortOrder))
        {
            var equipment = EquipmentPickList.FirstOrDefault(e => e.Id == item.EquipmentId);
            if (equipment is not null) BomItems.Add(PanelTemplateBomItemFormRow.FromDto(item, equipment));
        }
    }

    [RelayCommand]
    private void New()
    {
        SelectedTemplate = null;
        IsNew = true;
        IsEditing = true;
        FormTemplateCode = FormTemplateName = string.Empty;
        FormProductFamily = null;
        FormVoltageLevel = FormTechnicalDescription = FormNotes = null;
        FormPanelType = null;
        FormBodyEsTemplate = null;
        BomItems.Clear();
    }

    [RelayCommand] private void CancelEdit() => IsEditing = false;

    [RelayCommand]
    private void AddBomLine()
    {
        if (PickerSelectedEquipment is null) return;
        BomItems.Add(new PanelTemplateBomItemFormRow { Equipment = PickerSelectedEquipment, SortOrder = BomItems.Count + 1, Unit = PickerSelectedEquipment.Unit });
        PickerSelectedEquipment = null;
    }

    [RelayCommand] private void RemoveBomLine(PanelTemplateBomItemFormRow row) => BomItems.Remove(row);

    [RelayCommand]
    private async Task SaveAsync() => await RunBusyAsync(async () =>
    {
        if (FormProductFamily is null || FormPanelType is null)
        {
            ErrorMessage = "خانواده محصول و تیپ تابلو الزامی است.";
            return;
        }

        var items = BomItems.Select(r => r.ToRequest()).ToList();

        if (IsNew)
        {
            var created = await _api.CreateAsync(new CreatePanelTemplateRequest(
                FormTemplateCode, FormTemplateName, FormProductFamily.Id, FormVoltageLevel, FormPanelType.Id,
                FormTechnicalDescription, FormBodyEsTemplate?.Id, FormNotes, items));
            await SearchCoreAsync();
            SelectedTemplate = Templates.FirstOrDefault(t => t.Id == created.Id);
        }
        else if (SelectedTemplate is not null)
        {
            var updated = await _api.UpdateAsync(SelectedTemplate.Id, new UpdatePanelTemplateRequest(
                FormTemplateName, FormVoltageLevel, FormTechnicalDescription, FormBodyEsTemplate?.Id, FormNotes, items, SelectedTemplate.RowVersion));
            var index = Templates.IndexOf(SelectedTemplate);
            if (index >= 0) Templates[index] = updated;
            SelectedTemplate = updated;
        }
        IsEditing = false;
    });

    [RelayCommand]
    private async Task ApproveAsync() => await RunBusyAsync(async () =>
    {
        if (SelectedTemplate is null) return;
        if (!_dialogs.Confirm("تأیید تیپ تابلو", $"آیا تیپ تابلوی «{SelectedTemplate.TemplateName}» تأیید می‌شود؟")) return;
        var approved = await _api.ApproveAsync(SelectedTemplate.Id, new ApproveTemplateRequest(SelectedTemplate.RowVersion));
        var index = Templates.IndexOf(SelectedTemplate);
        if (index >= 0) Templates[index] = approved;
        SelectedTemplate = approved;
    });

    [RelayCommand]
    private async Task CreateNewRevisionAsync() => await RunBusyAsync(async () =>
    {
        if (SelectedTemplate is null) return;
        var revised = await _api.CreateNewRevisionAsync(SelectedTemplate.Id);
        await SearchCoreAsync();
        SelectedTemplate = Templates.FirstOrDefault(t => t.Id == revised.Id);
    });
}
