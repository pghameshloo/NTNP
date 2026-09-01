using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using NTNP.Pricing.Application.Common;
using NTNP.Pricing.Application.Equipment;
using NTNP.Pricing.Contracts.Equipment;
using NTNP.Pricing.Domain.Calculation;
using NTNP.Pricing.Domain.Entities;
using NTNP.Pricing.Domain.Enums;
using NTNP.Pricing.Infrastructure.Persistence;

namespace NTNP.Pricing.Infrastructure.Excel;

/// <summary>
/// Section 9's 10-step Excel import workflow for the Equipment Database. Expected columns (row 1 =
/// header, case-insensitive): Code, PartNumber, DescriptionFa, DescriptionEn, Category, Subcategory,
/// Brand, Model, Manufacturer, Supplier, Unit, PurchaseCurrency, ForeignUnitPrice, RialUnitPrice,
/// EffectiveDate, PriceSource, Notes.
/// </summary>
public sealed class EquipmentImportService : IEquipmentImportService
{
    private static readonly string[] ExpectedHeaders =
    {
        "Code", "PartNumber", "DescriptionFa", "DescriptionEn", "Category", "Subcategory", "Brand", "Model",
        "Manufacturer", "Supplier", "Unit", "PurchaseCurrency", "ForeignUnitPrice", "RialUnitPrice",
        "EffectiveDate", "PriceSource", "Notes",
    };

    private readonly ApplicationDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly IFileStorageService _fileStorage;
    private readonly IAuditService _audit;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public EquipmentImportService(
        ApplicationDbContext db, IMemoryCache cache, IFileStorageService fileStorage, IAuditService audit,
        ICurrentUserService currentUser, IDateTimeProvider clock)
    {
        _db = db;
        _cache = cache;
        _fileStorage = fileStorage;
        _audit = audit;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<EquipmentImportPreviewResult> PreviewAsync(Stream fileContent, string fileName, CancellationToken ct = default)
    {
        using var memoryStream = new MemoryStream();
        await fileContent.CopyToAsync(memoryStream, ct);
        var fileBytes = memoryStream.ToArray();

        using var workbook = new XLWorkbook(new MemoryStream(fileBytes));
        var sheet = workbook.Worksheets.First();

        var headerRow = sheet.Row(1);
        var columnIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var cell in headerRow.CellsUsed())
        {
            var header = cell.GetString().Trim();
            if (!string.IsNullOrEmpty(header)) columnIndex[header] = cell.Address.ColumnNumber;
        }

        var missingHeaders = ExpectedHeaders.Where(h => !columnIndex.ContainsKey(h)).Where(h => h == "Code").ToList();
        if (missingHeaders.Count > 0)
            throw new Domain.Exceptions.DomainValidationException($"Missing required column(s): {string.Join(", ", missingHeaders)}");

        var existingCodes = await _db.Equipment.Select(e => e.Code).ToListAsync(ct);
        var existingCurrencyCodes = await _db.Currencies.Select(c => c.Code).ToListAsync(ct);

        var rows = new List<(EquipmentImportRowPreview Preview, ParsedEquipmentRow? Parsed)>();
        var seenCodesInFile = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;
        for (var rowNum = 2; rowNum <= lastRow; rowNum++)
        {
            var row = sheet.Row(rowNum);
            if (row.IsEmpty()) continue;

            string? Get(string col) => columnIndex.TryGetValue(col, out var idx) ? row.Cell(idx).GetString().Trim() : null;

            var code = Get("Code");
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(code)) errors.Add("Equipment Code is required.");
            else if (!seenCodesInFile.Add(code)) errors.Add($"Duplicate Equipment Code '{code}' within this file.");

            var currencyCode = string.IsNullOrWhiteSpace(Get("PurchaseCurrency")) ? "IRR" : Get("PurchaseCurrency")!.ToUpperInvariant();
            if (!existingCurrencyCodes.Contains(currencyCode))
                errors.Add($"Unknown purchase currency '{currencyCode}'. Add it under Currencies first.");

            decimal? foreignPrice = TryParseDecimal(Get("ForeignUnitPrice"));
            decimal? rialPrice = TryParseDecimal(Get("RialUnitPrice"));

            if (currencyCode == "IRR")
            {
                if (foreignPrice is > 0) errors.Add("Foreign unit price must be empty when purchase currency is IRR.");
                if (rialPrice is null or <= 0) errors.Add("Rial unit price is required and must be positive for IRR currency.");
            }
            else
            {
                if (foreignPrice is null or <= 0) errors.Add("Foreign unit price is required and must be positive for a non-IRR currency.");
            }

            var isUpdate = code is not null && existingCodes.Contains(code);

            rows.Add((
                new EquipmentImportRowPreview(rowNum, code, Get("DescriptionEn"), currencyCode, foreignPrice, rialPrice, isUpdate, errors),
                errors.Count > 0 || code is null ? null : new ParsedEquipmentRow(
                    code, Get("PartNumber"), Get("DescriptionFa") ?? string.Empty, Get("DescriptionEn") ?? string.Empty,
                    Get("Category"), Get("Subcategory"), Get("Brand"), Get("Model"), Get("Manufacturer"), Get("Supplier"),
                    string.IsNullOrWhiteSpace(Get("Unit")) ? "EA" : Get("Unit")!, currencyCode, foreignPrice, rialPrice,
                    TryParseDate(Get("EffectiveDate")) ?? _clock.UtcNow, Get("PriceSource"), Get("Notes"), isUpdate)));
        }

        var token = Guid.NewGuid().ToString("N");
        _cache.Set(CacheKey(token), new CachedImport(fileBytes, fileName, rows.Select(r => r.Parsed).Where(p => p is not null).Select(p => p!).ToList()),
            TimeSpan.FromMinutes(30));

        var previews = rows.Select(r => r.Preview).ToList();
        return new EquipmentImportPreviewResult(
            previews, previews.Count(p => !p.IsUpdate && p.Errors.Count == 0), previews.Count(p => p.IsUpdate && p.Errors.Count == 0),
            previews.Count(p => p.Errors.Count > 0), token);
    }

    public async Task<EquipmentImportCommitResult> CommitAsync(EquipmentImportCommitRequest request, CancellationToken ct = default)
    {
        if (!_cache.TryGetValue(CacheKey(request.ImportToken), out CachedImport? cached) || cached is null)
            throw new Domain.Exceptions.DomainValidationException("Import session expired or not found — re-upload and preview the file again.");

        var currencies = await _db.Currencies.ToDictionaryAsync(c => c.Code, ct);
        var purchaseRates = await _db.ExchangeRates.Where(r => r.IsActive).ToListAsync(ct);

        var inserted = 0;
        var updated = 0;

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        foreach (var row in cached.Rows)
        {
            var equipment = await _db.Equipment.Include(e => e.Prices).FirstOrDefaultAsync(e => e.Code == row.Code, ct);
            if (equipment is null)
            {
                equipment = new Domain.Entities.Equipment
                {
                    Code = row.Code,
                    CreatedByUserId = _currentUser.UserId,
                    CreatedByUserName = _currentUser.UserName,
                };
                _db.Equipment.Add(equipment);
                inserted++;
            }
            else
            {
                updated++;
            }

            equipment.TechnicalPartNumber = row.PartNumber;
            equipment.DescriptionFa = row.DescriptionFa;
            equipment.DescriptionEn = row.DescriptionEn;
            equipment.Category = row.Category;
            equipment.Subcategory = row.Subcategory;
            equipment.Brand = row.Brand;
            equipment.Model = row.Model;
            equipment.Manufacturer = row.Manufacturer;
            equipment.Supplier = row.Supplier;
            equipment.Unit = row.Unit;
            equipment.UpdatedByUserId = _currentUser.UserId;
            equipment.UpdatedByUserName = _currentUser.UserName;
            equipment.UpdatedAtUtc = _clock.UtcNow;

            await _db.SaveChangesAsync(ct); // ensure equipment.Id is available for the price FK

            decimal? purchaseRate = null;
            if (row.PurchaseCurrencyCode != "IRR")
            {
                purchaseRate = purchaseRates
                    .Where(r => currencies.TryGetValue(row.PurchaseCurrencyCode, out var c) && r.CurrencyId == c.Id)
                    .OrderByDescending(r => r.EffectiveAtUtc)
                    .FirstOrDefault()?.PurchaseRateToIrr;
            }

            var finalCost = PricingCalculationEngine.CalculateEquipmentFinalUnitCostIrr(
                row.PurchaseCurrencyCode, row.ForeignUnitPrice, row.RialUnitPrice, purchaseRate);

            _db.EquipmentPrices.Add(new EquipmentPrice
            {
                EquipmentId = equipment.Id,
                PurchaseCurrencyCode = row.PurchaseCurrencyCode,
                ForeignUnitPrice = row.ForeignUnitPrice,
                RialUnitPrice = row.RialUnitPrice,
                PurchaseExchangeRateSnapshot = purchaseRate,
                FinalUnitCostIrr = finalCost,
                EffectiveAtUtc = row.EffectiveAtUtc,
                PriceSourceText = row.PriceSource ?? "Excel import",
                Notes = row.Notes,
                IsActive = true,
                CreatedByUserId = _currentUser.UserId,
                CreatedByUserName = _currentUser.UserName,
            });
        }

        await _db.SaveChangesAsync(ct);

        var stored = await _fileStorage.SaveAsync(cached.FileName, new MemoryStream(cached.FileBytes), ct);
        var storedFile = new StoredFile
        {
            FileName = cached.FileName,
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            Category = FileCategory.ImportedExcel,
            StoragePath = stored.StoragePath,
            SizeBytes = stored.SizeBytes,
            Sha256Hash = stored.Sha256Hash,
            CreatedByUserId = _currentUser.UserId,
            CreatedByUserName = _currentUser.UserName,
        };
        _db.StoredFiles.Add(storedFile);
        await _db.SaveChangesAsync(ct);

        await transaction.CommitAsync(ct);

        _cache.Remove(CacheKey(request.ImportToken));

        await _audit.LogAsync(AuditAction.Imported, nameof(Domain.Entities.Equipment), storedFile.Id.ToString(),
            newValue: new { Inserted = inserted, Updated = updated, cached.FileName }, cancellationToken: ct);

        return new EquipmentImportCommitResult(inserted, updated, storedFile.Id);
    }

    private static string CacheKey(string token) => $"equipment-import:{token}";

    private static decimal? TryParseDecimal(string? value) =>
        decimal.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var result) ? result : null;

    private static DateTimeOffset? TryParseDate(string? value) =>
        DateTimeOffset.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var result)
            ? result : null;

    private sealed record ParsedEquipmentRow(
        string Code, string? PartNumber, string DescriptionFa, string DescriptionEn, string? Category, string? Subcategory,
        string? Brand, string? Model, string? Manufacturer, string? Supplier, string Unit, string PurchaseCurrencyCode,
        decimal? ForeignUnitPrice, decimal? RialUnitPrice, DateTimeOffset EffectiveAtUtc, string? PriceSource, string? Notes, bool IsUpdate);

    private sealed record CachedImport(byte[] FileBytes, string FileName, IReadOnlyList<ParsedEquipmentRow> Rows);
}
