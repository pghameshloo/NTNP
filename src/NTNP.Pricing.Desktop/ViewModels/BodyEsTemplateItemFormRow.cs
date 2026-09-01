using CommunityToolkit.Mvvm.ComponentModel;
using NTNP.Pricing.Contracts.BodyEs;

namespace NTNP.Pricing.Desktop.ViewModels;

/// <summary>One editable row of the BODY+ES template editor (Section 22 screen 9, Section 11).</summary>
public sealed partial class BodyEsTemplateItemFormRow : ObservableObject
{
    public Guid? Id { get; init; }

    [ObservableProperty] private string _componentCode = string.Empty;
    [ObservableProperty] private string _descriptionFa = string.Empty;
    [ObservableProperty] private string? _descriptionEn;
    [ObservableProperty] private string? _category;
    [ObservableProperty] private string _unit = "EA";
    [ObservableProperty] private decimal _quantityPerPanel = 1m;
    [ObservableProperty] private decimal _wastePercentage;
    [ObservableProperty] private decimal _unitCostIrr;
    [ObservableProperty] private string? _notes;
    [ObservableProperty] private int _sortOrder;

    public static BodyEsTemplateItemFormRow FromDto(BodyEsTemplateItemDto dto) => new()
    {
        Id = dto.Id,
        ComponentCode = dto.ComponentCode,
        DescriptionFa = dto.DescriptionFa,
        DescriptionEn = dto.DescriptionEn,
        Category = dto.Category,
        Unit = dto.Unit,
        QuantityPerPanel = dto.QuantityPerPanel,
        WastePercentage = dto.WastePercentage,
        UnitCostIrr = dto.UnitCostIrr,
        Notes = dto.Notes,
        SortOrder = dto.SortOrder,
    };

    public UpsertBodyEsTemplateItemRequest ToRequest() =>
        new(Id, ComponentCode, DescriptionFa, DescriptionEn, Category, Unit, QuantityPerPanel, WastePercentage, UnitCostIrr, Notes, SortOrder);
}
