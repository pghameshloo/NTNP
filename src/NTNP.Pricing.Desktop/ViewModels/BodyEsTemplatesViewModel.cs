using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NTNP.Pricing.Contracts.BodyEs;
using NTNP.Pricing.Contracts.PanelTemplates;
using NTNP.Pricing.Desktop.Services;
using NTNP.Pricing.Desktop.Services.Api;

namespace NTNP.Pricing.Desktop.ViewModels;

/// <summary>Screen 9 (Section 22/11) — BODY+ES Templates.</summary>
public sealed partial class BodyEsTemplatesViewModel : ViewModelBase
{
    private readonly BodyEsTemplatesApiClient _api;
    private readonly LookupApiClient _lookups;
    private readonly IDialogService _dialogs;

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private BodyEsTemplateDto? _selectedTemplate;
    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private bool _isNew;

    [ObservableProperty] private string _formTemplateCode = string.Empty;
    [ObservableProperty] private string _formTemplateName = string.Empty;
    [ObservableProperty] private ProductFamilyDto? _formProductFamily;
    [ObservableProperty] private PanelTypeDto? _formPanelType;
    [ObservableProperty] private string? _formPanelDimensions;
    [ObservableProperty] private string? _formNotes;

    public ObservableCollection<BodyEsTemplateDto> Templates { get; } = new();
    public ObservableCollection<ProductFamilyDto> ProductFamilies { get; } = new();
    public ObservableCollection<PanelTypeDto> PanelTypes { get; } = new();
    public ObservableCollection<BodyEsTemplateItemFormRow> Items { get; } = new();

    public BodyEsTemplatesViewModel(BodyEsTemplatesApiClient api, LookupApiClient lookups, IDialogService dialogs)
    {
        _api = api;
        _lookups = lookups;
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
        });
        await SearchAsync();
    }

    [RelayCommand]
    private async Task SearchAsync() => await RunBusyAsync(async () =>
    {
        var page = await _api.SearchAsync(SearchText, 1, 200, null, null);
        Templates.Clear();
        foreach (var t in page.Items) Templates.Add(t);
    });

    partial void OnSelectedTemplateChanged(BodyEsTemplateDto? value)
    {
        Items.Clear();
        if (value is null) return;
        IsNew = false;
        IsEditing = true;
        FormTemplateCode = value.TemplateCode;
        FormTemplateName = value.TemplateName;
        FormProductFamily = ProductFamilies.FirstOrDefault(f => f.Id == value.ProductFamilyId);
        FormPanelType = PanelTypes.FirstOrDefault(t => t.Id == value.PanelTypeId);
        FormPanelDimensions = value.PanelDimensions;
        FormNotes = value.Notes;
        foreach (var item in value.Items.OrderBy(i => i.SortOrder)) Items.Add(BodyEsTemplateItemFormRow.FromDto(item));
    }

    [RelayCommand]
    private void New()
    {
        SelectedTemplate = null;
        IsNew = true;
        IsEditing = true;
        FormTemplateCode = FormTemplateName = string.Empty;
        FormProductFamily = null;
        FormPanelType = null;
        FormPanelDimensions = FormNotes = null;
        Items.Clear();
    }

    [RelayCommand] private void CancelEdit() => IsEditing = false;

    [RelayCommand]
    private void AddItemLine() => Items.Add(new BodyEsTemplateItemFormRow { SortOrder = Items.Count + 1 });

    [RelayCommand] private void RemoveItemLine(BodyEsTemplateItemFormRow row) => Items.Remove(row);

    [RelayCommand]
    private async Task SaveAsync() => await RunBusyAsync(async () =>
    {
        if (FormProductFamily is null || FormPanelType is null)
        {
            ErrorMessage = "خانواده محصول و تیپ تابلو الزامی است.";
            return;
        }

        var items = Items.Select(r => r.ToRequest()).ToList();

        if (IsNew)
        {
            var created = await _api.CreateAsync(new CreateBodyEsTemplateRequest(FormTemplateCode, FormTemplateName, FormProductFamily.Id, FormPanelType.Id, FormPanelDimensions, FormNotes, items));
            await SearchAsync();
            SelectedTemplate = Templates.FirstOrDefault(t => t.Id == created.Id);
        }
        else if (SelectedTemplate is not null)
        {
            var updated = await _api.UpdateAsync(SelectedTemplate.Id, new UpdateBodyEsTemplateRequest(FormTemplateName, FormPanelDimensions, FormNotes, items, SelectedTemplate.RowVersion));
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
        if (!_dialogs.Confirm("تأیید قالب BODY+ES", $"آیا قالب «{SelectedTemplate.TemplateName}» تأیید می‌شود؟")) return;
        var approved = await _api.ApproveAsync(SelectedTemplate.Id, SelectedTemplate.RowVersion);
        var index = Templates.IndexOf(SelectedTemplate);
        if (index >= 0) Templates[index] = approved;
        SelectedTemplate = approved;
    });
}
