using CommunityToolkit.Mvvm.ComponentModel;
using NTNP.Pricing.Contracts.Equipment;
using NTNP.Pricing.Contracts.PanelTemplates;

namespace NTNP.Pricing.Desktop.ViewModels;

/// <summary>
/// One editable row of the Panel BOM Editor (Section 22 screen 8). Panel templates are saved as a
/// full BOM replace (Create/UpdatePanelTemplateRequest.BomItems), so the form just maintains this
/// list locally and converts it to <see cref="UpsertPanelTemplateBomItemRequest"/> on Save — no
/// separate per-line API calls are needed.
/// </summary>
public sealed partial class PanelTemplateBomItemFormRow : ObservableObject
{
    public Guid? Id { get; init; }

    [ObservableProperty] private EquipmentDto? _equipment;
    [ObservableProperty] private decimal _quantityPerPanel = 1m;
    [ObservableProperty] private string _unit = "EA";
    [ObservableProperty] private decimal _wastePercentage;
    [ObservableProperty] private decimal? _costMultiplier;
    [ObservableProperty] private string? _notes;
    [ObservableProperty] private int _sortOrder;

    public static PanelTemplateBomItemFormRow FromDto(PanelTemplateBomItemDto dto, EquipmentDto equipment) => new()
    {
        Id = dto.Id,
        Equipment = equipment,
        QuantityPerPanel = dto.QuantityPerPanel,
        Unit = dto.Unit,
        WastePercentage = dto.WastePercentage,
        CostMultiplier = dto.CostMultiplier,
        Notes = dto.Notes,
        SortOrder = dto.SortOrder,
    };

    public UpsertPanelTemplateBomItemRequest ToRequest() =>
        new(Id, Equipment!.Id, QuantityPerPanel, Unit, WastePercentage, CostMultiplier, Notes, SortOrder);
}
