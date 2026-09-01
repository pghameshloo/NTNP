namespace NTNP.Pricing.Contracts.Equipment;

public sealed record EquipmentDto(
    Guid Id,
    string Code,
    string? TechnicalPartNumber,
    string DescriptionFa,
    string DescriptionEn,
    string? Category,
    string? Subcategory,
    string? Brand,
    string? Model,
    string? Manufacturer,
    string? Supplier,
    string Unit,
    int? LeadTimeDays,
    string? Notes,
    bool IsActive,
    EquipmentPriceDto? CurrentPrice,
    bool HasMissingPrice,
    bool HasExpiredPrice,
    byte[] RowVersion);

public sealed record CreateEquipmentRequest(
    string Code, string? TechnicalPartNumber, string DescriptionFa, string DescriptionEn,
    string? Category, string? Subcategory, string? Brand, string? Model, string? Manufacturer,
    string? Supplier, string Unit, int? LeadTimeDays, string? Notes);

public sealed record UpdateEquipmentRequest(
    string? TechnicalPartNumber, string DescriptionFa, string DescriptionEn,
    string? Category, string? Subcategory, string? Brand, string? Model, string? Manufacturer,
    string? Supplier, string Unit, int? LeadTimeDays, string? Notes, bool IsActive, byte[] RowVersion);

public sealed record EquipmentPriceDto(
    Guid Id,
    Guid EquipmentId,
    string PurchaseCurrencyCode,
    decimal? ForeignUnitPrice,
    decimal? RialUnitPrice,
    decimal? PurchaseExchangeRateSnapshot,
    decimal FinalUnitCostIrr,
    DateTimeOffset EffectiveAtUtc,
    string? PriceSourceText,
    string? Notes,
    bool IsActive,
    string CreatedByUserName,
    DateTimeOffset CreatedAtUtc);

public sealed record CreateEquipmentPriceRequest(
    Guid EquipmentId,
    string PurchaseCurrencyCode,
    decimal? ForeignUnitPrice,
    decimal? RialUnitPrice,
    DateTimeOffset EffectiveAtUtc,
    string? PriceSourceText,
    string? Notes);

public sealed record EquipmentImportRowPreview(
    int RowNumber,
    string? Code,
    string? DescriptionEn,
    string? PurchaseCurrencyCode,
    decimal? ForeignUnitPrice,
    decimal? RialUnitPrice,
    bool IsUpdate,
    IReadOnlyList<string> Errors);

public sealed record EquipmentImportPreviewResult(
    IReadOnlyList<EquipmentImportRowPreview> Rows,
    int InsertCount,
    int UpdateCount,
    int ErrorCount,
    string ImportToken);

public sealed record EquipmentImportCommitRequest(string ImportToken);

public sealed record EquipmentImportCommitResult(int InsertedCount, int UpdatedCount, Guid StoredFileId);
