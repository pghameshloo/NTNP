namespace NTNP.Pricing.Domain.Enums;

/// <summary>Applies to PanelTemplate and BodyEsTemplate (Sections 10/11: versioning and approval).</summary>
public enum TemplateStatus
{
    Draft = 1,
    Approved = 2,
    Deprecated = 3,
}

/// <summary>Applies to Equipment/EquipmentPrice and general master-data "source" provenance.</summary>
public enum PriceSource
{
    ManualEntry = 1,
    ExcelImport = 2,
    SupplierQuotation = 3,
    Other = 4,
}
