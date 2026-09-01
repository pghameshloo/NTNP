using Microsoft.EntityFrameworkCore;
using NTNP.Pricing.Domain.Entities;
using NTNP.Pricing.Domain.Enums;
using NTNP.Pricing.Infrastructure.Persistence;

namespace NTNP.Pricing.Infrastructure.Seed;

/// <summary>
/// Section 37 — master data: currencies + sample rates, the default 85/15 EUR pricing profile,
/// product families, panel types, sample equipment, one panel template, one BODY+ES template and
/// one sample customer. Idempotent: safe to call on every application start.
/// </summary>
public static class MasterDataSeeder
{
    private const string SeedUserName = "system-seed";
    private static readonly Guid SeedUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public static async Task<SeedResult> SeedAsync(ApplicationDbContext db, CancellationToken ct = default)
    {
        var currencies = await SeedCurrenciesAsync(db, ct);
        var pricingProfile = await SeedPricingProfileAsync(db, ct);
        var (uniSafe, uniGear, sivacon) = await SeedProductFamiliesAsync(db, ct);
        var panelTypes = await SeedPanelTypesAsync(db, ct);
        var (acb, relay) = await SeedEquipmentAsync(db, currencies["EUR"], ct);
        var bodyEs = await SeedBodyEsTemplateAsync(db, uniSafe, panelTypes["INCOMING"], ct);
        var panelTemplate = await SeedPanelTemplateAsync(db, uniSafe, panelTypes["INCOMING"], acb, relay, ct);
        var customer = await SeedCustomerAsync(db, ct);
        await db.SaveChangesAsync(ct);

        return new SeedResult(currencies, pricingProfile, uniSafe, panelTypes["INCOMING"], acb, relay, panelTemplate, bodyEs, customer);
    }

    private static async Task<Dictionary<string, Currency>> SeedCurrenciesAsync(ApplicationDbContext db, CancellationToken ct)
    {
        var seedSpecs = new (string Code, string Name, string Symbol, bool IsBase, decimal? Purchase, decimal? Selling)[]
        {
            ("IRR", "Iranian Rial", "﷼", true, null, null),
            ("EUR", "Euro", "€", false, 1_800_000m, 1_800_000m), // matches Section 20's mandatory scenario exactly
            ("USD", "US Dollar", "$", false, 1_650_000m, 1_660_000m),
            ("CNY", "Chinese Yuan", "¥", false, 230_000m, 232_000m),
            ("AED", "UAE Dirham", "AED", false, 449_000m, 452_000m),
        };

        var result = new Dictionary<string, Currency>();
        foreach (var spec in seedSpecs)
        {
            var currency = await db.Currencies.FirstOrDefaultAsync(c => c.Code == spec.Code, ct);
            if (currency is null)
            {
                currency = new Currency
                {
                    Code = spec.Code,
                    Name = spec.Name,
                    Symbol = spec.Symbol,
                    IsBaseCurrency = spec.IsBase,
                    CreatedByUserId = SeedUserId,
                    CreatedByUserName = SeedUserName,
                };
                db.Currencies.Add(currency);
                await db.SaveChangesAsync(ct);
            }

            result[spec.Code] = currency;

            if (spec.Purchase is not null && !await db.ExchangeRates.AnyAsync(r => r.CurrencyId == currency.Id, ct))
            {
                db.ExchangeRates.Add(new ExchangeRate
                {
                    CurrencyId = currency.Id,
                    PurchaseRateToIrr = spec.Purchase.Value,
                    SellingRateToIrr = spec.Selling!.Value,
                    EffectiveAtUtc = DateTimeOffset.UtcNow,
                    RateSource = "Seed data",
                    IsActive = true,
                    CreatedByUserId = SeedUserId,
                    CreatedByUserName = SeedUserName,
                });
            }
        }

        return result;
    }

    private static async Task<PricingProfile> SeedPricingProfileAsync(ApplicationDbContext db, CancellationToken ct)
    {
        const string name = "Default 85/15 EUR Profile";
        var profile = await db.PricingProfiles.FirstOrDefaultAsync(p => p.Name == name, ct);
        if (profile is not null) return profile;

        profile = new PricingProfile
        {
            Name = name,
            PricingMethod = PricingMethod.Markup,
            DefaultRate = 0.30m,
            DefaultForeignShare = 0.85m,
            DefaultRialShare = 0.15m,
            DefaultQuotationCurrencyCode = "EUR",
            IrrRoundingPolicy = RoundingMode.NearestThousand,
            ForeignRoundingPolicy = RoundingMode.NearestInteger,
            ForeignDecimalPlaces = 2,
            ReconciliationToleranceIrr = 1m,
            CreatedByUserId = SeedUserId,
            CreatedByUserName = SeedUserName,
        };
        db.PricingProfiles.Add(profile);
        return profile;
    }

    private static async Task<(ProductFamily UniSafe, ProductFamily UniGear, ProductFamily Sivacon)> SeedProductFamiliesAsync(
        ApplicationDbContext db, CancellationToken ct)
    {
        var specs = new (string Code, string Name, string Voltage, string Cls)[]
        {
            ("UNISAFE", "UniSafe", "6–24 kV", "MV withdrawable switchgear"),
            ("UNIGEAR-ZS32", "UniGear ZS3.2", "33–40.5 kV", "MV withdrawable switchgear"),
            ("SIVACON-8PT", "SIVACON 8PT", "up to 690 V", "LV withdrawable switchgear"),
        };

        var byCode = new Dictionary<string, ProductFamily>();
        foreach (var s in specs)
        {
            var pf = await db.ProductFamilies.FirstOrDefaultAsync(x => x.Code == s.Code, ct);
            if (pf is null)
            {
                pf = new ProductFamily
                {
                    Code = s.Code,
                    Name = s.Name,
                    VoltageRangeDescription = s.Voltage,
                    SwitchgearClass = s.Cls,
                    CreatedByUserId = SeedUserId,
                    CreatedByUserName = SeedUserName,
                };
                db.ProductFamilies.Add(pf);
                await db.SaveChangesAsync(ct);
            }
            byCode[s.Code] = pf;
        }

        return (byCode["UNISAFE"], byCode["UNIGEAR-ZS32"], byCode["SIVACON-8PT"]);
    }

    private static async Task<Dictionary<string, PanelType>> SeedPanelTypesAsync(ApplicationDbContext db, CancellationToken ct)
    {
        var codes = new[] { "INCOMING", "OUTGOING", "BUS COUPLER", "BUS RISER", "METERING", "AUXILIARY", "CUSTOM" };
        var result = new Dictionary<string, PanelType>();
        var order = 0;
        foreach (var code in codes)
        {
            var pt = await db.PanelTypes.FirstOrDefaultAsync(x => x.Code == code, ct);
            if (pt is null)
            {
                pt = new PanelType
                {
                    Code = code,
                    Name = code,
                    SortOrder = order,
                    CreatedByUserId = SeedUserId,
                    CreatedByUserName = SeedUserName,
                };
                db.PanelTypes.Add(pt);
                await db.SaveChangesAsync(ct);
            }
            result[code] = pt;
            order++;
        }
        return result;
    }

    private static async Task<(Equipment Acb, Equipment Relay)> SeedEquipmentAsync(
        ApplicationDbContext db, Currency eur, CancellationToken ct)
    {
        var acb = await GetOrCreateEquipmentAsync(db, "ACB-001", "کلید هوایی خودکار", "Air Circuit Breaker",
            "Circuit Breakers", "MV", "Generic", "ACB-4000", "Generic Manufacturer Co.", "EA", ct);
        await EnsurePriceAsync(db, acb, "EUR", 800m, null, 1_800_000m, ct);

        var relay = await GetOrCreateEquipmentAsync(db, "RLY-001", "رله حفاظتی", "Protection Relay",
            "Protection & Control", "Numerical Relay", "Generic", "RLY-P3", "Generic Manufacturer Co.", "EA", ct);
        await EnsurePriceAsync(db, relay, "IRR", null, 50_000_000m, null, ct);

        // A handful of additional catalog items so the Equipment Database screen is not a two-row demo.
        var ct1 = await GetOrCreateEquipmentAsync(db, "CT-001", "ترانس جریان", "Current Transformer",
            "Instrument Transformers", "CT", "Generic", "CT-600/5", "Generic Manufacturer Co.", "EA", ct);
        await EnsurePriceAsync(db, ct1, "IRR", null, 18_500_000m, null, ct);

        var vt1 = await GetOrCreateEquipmentAsync(db, "VT-001", "ترانس ولتاژ", "Voltage Transformer",
            "Instrument Transformers", "VT", "Generic", "VT-20/0.1", "Generic Manufacturer Co.", "EA", ct);
        await EnsurePriceAsync(db, vt1, "EUR", 210m, null, 1_800_000m, ct);

        var contactor = await GetOrCreateEquipmentAsync(db, "CTC-001", "کنتاکتور", "Vacuum Contactor",
            "Switching Devices", "Contactor", "Generic", "VC-400", "Generic Manufacturer Co.", "EA", ct);
        await EnsurePriceAsync(db, contactor, "USD", 260m, null, 1_650_000m, ct);

        await db.SaveChangesAsync(ct);
        return (acb, relay);
    }

    private static async Task<Equipment> GetOrCreateEquipmentAsync(
        ApplicationDbContext db, string code, string descFa, string descEn, string category, string subcategory,
        string brand, string model, string manufacturer, string unit, CancellationToken ct)
    {
        var eq = await db.Equipment.FirstOrDefaultAsync(x => x.Code == code, ct);
        if (eq is not null) return eq;

        eq = new Equipment
        {
            Code = code,
            DescriptionFa = descFa,
            DescriptionEn = descEn,
            Category = category,
            Subcategory = subcategory,
            Brand = brand,
            Model = model,
            Manufacturer = manufacturer,
            Unit = unit,
            LeadTimeDays = 60,
            CreatedByUserId = SeedUserId,
            CreatedByUserName = SeedUserName,
        };
        db.Equipment.Add(eq);
        await db.SaveChangesAsync(ct);
        return eq;
    }

    private static async Task EnsurePriceAsync(
        ApplicationDbContext db, Equipment equipment, string currencyCode,
        decimal? foreignUnitPrice, decimal? rialUnitPrice, decimal? purchaseRate, CancellationToken ct)
    {
        if (await db.EquipmentPrices.AnyAsync(p => p.EquipmentId == equipment.Id, ct)) return;

        var finalCostIrr = Domain.Calculation.PricingCalculationEngine.CalculateEquipmentFinalUnitCostIrr(
            currencyCode, foreignUnitPrice, rialUnitPrice, purchaseRate);

        db.EquipmentPrices.Add(new EquipmentPrice
        {
            EquipmentId = equipment.Id,
            PurchaseCurrencyCode = currencyCode,
            ForeignUnitPrice = foreignUnitPrice,
            RialUnitPrice = rialUnitPrice,
            PurchaseExchangeRateSnapshot = purchaseRate,
            FinalUnitCostIrr = finalCostIrr,
            EffectiveAtUtc = DateTimeOffset.UtcNow,
            PriceSourceText = "Seed data",
            IsActive = true,
            CreatedByUserId = SeedUserId,
            CreatedByUserName = SeedUserName,
        });
    }

    private static async Task<BodyEsTemplate> SeedBodyEsTemplateAsync(
        ApplicationDbContext db, ProductFamily productFamily, PanelType panelType, CancellationToken ct)
    {
        const string code = "BES-UNISAFE-INC-001";
        var existing = await db.BodyEsTemplates.FirstOrDefaultAsync(x => x.TemplateCode == code, ct);
        if (existing is not null) return existing;

        var template = new BodyEsTemplate
        {
            TemplateCode = code,
            TemplateName = "UniSafe INCOMING — Body & Sheet Metal",
            ProductFamilyId = productFamily.Id,
            PanelTypeId = panelType.Id,
            PanelDimensions = "800x1500x2350 mm",
            RevisionNumber = 1,
            Status = TemplateStatus.Approved,
            CreatedByUserId = SeedUserId,
            CreatedByUserName = SeedUserName,
        };
        template.Items.Add(new BodyEsTemplateItem
        {
            ComponentCode = "SHT-001", DescriptionFa = "بدنه فلزی", DescriptionEn = "Sheet metal enclosure",
            Category = "Body", Unit = "SET", QuantityPerPanel = 1m, WastePercentage = 0.03m, UnitCostIrr = 320_000_000m, SortOrder = 1,
        });
        template.Items.Add(new BodyEsTemplateItem
        {
            ComponentCode = "PNT-001", DescriptionFa = "رنگ الکترواستاتیک", DescriptionEn = "Electrostatic powder coating",
            Category = "Finishing", Unit = "M2", QuantityPerPanel = 6m, WastePercentage = 0.05m, UnitCostIrr = 4_500_000m, SortOrder = 2,
        });
        db.BodyEsTemplates.Add(template);
        await db.SaveChangesAsync(ct);
        return template;
    }

    private static async Task<PanelTemplate> SeedPanelTemplateAsync(
        ApplicationDbContext db, ProductFamily productFamily, PanelType panelType,
        Equipment acb, Equipment relay, CancellationToken ct)
    {
        const string code = "PT-UNISAFE-INC-001";
        var existing = await db.PanelTemplates.FirstOrDefaultAsync(x => x.TemplateCode == code, ct);
        if (existing is not null) return existing;

        // Deliberately mirrors the Section 20 mandatory calculation scenario quantities exactly
        // (2x ACB, 3x Relay, no waste) so the seeded sample project reproduces those numbers.
        var template = new PanelTemplate
        {
            TemplateCode = code,
            TemplateName = "UniSafe INCOMING Panel",
            ProductFamilyId = productFamily.Id,
            PanelTypeId = panelType.Id,
            VoltageLevel = "20 kV",
            TechnicalDescription = "UniSafe MV withdrawable INCOMING panel, standard equipment set.",
            RevisionNumber = 1,
            Status = TemplateStatus.Approved,
            ApprovedByUserId = SeedUserId,
            ApprovedByUserName = SeedUserName,
            ApprovedAtUtc = DateTimeOffset.UtcNow,
            CreatedByUserId = SeedUserId,
            CreatedByUserName = SeedUserName,
        };
        template.BomItems.Add(new PanelTemplateBomItem { EquipmentId = acb.Id, QuantityPerPanel = 2m, Unit = "EA", WastePercentage = 0m, SortOrder = 1 });
        template.BomItems.Add(new PanelTemplateBomItem { EquipmentId = relay.Id, QuantityPerPanel = 3m, Unit = "EA", WastePercentage = 0m, SortOrder = 2 });
        db.PanelTemplates.Add(template);
        await db.SaveChangesAsync(ct);
        return template;
    }

    private static async Task<Customer> SeedCustomerAsync(ApplicationDbContext db, CancellationToken ct)
    {
        const string code = "CUST-0001";
        var existing = await db.Customers.FirstOrDefaultAsync(x => x.CustomerCode == code, ct);
        if (existing is not null) return existing;

        var customer = new Customer
        {
            CustomerCode = code,
            CompanyName = "Sample Industries Co.",
            Industry = "Power Distribution",
            ContactPerson = "Ali Rezaei",
            ContactPosition = "Procurement Manager",
            Phone = "+98-21-00000000",
            Email = "procurement@sample-industries.example",
            Address = "Tehran, Iran",
            CreatedByUserId = SeedUserId,
            CreatedByUserName = SeedUserName,
        };
        db.Customers.Add(customer);
        await db.SaveChangesAsync(ct);
        return customer;
    }
}

public sealed record SeedResult(
    Dictionary<string, Currency> Currencies,
    PricingProfile PricingProfile,
    ProductFamily UniSafe,
    PanelType IncomingPanelType,
    Equipment Acb,
    Equipment Relay,
    PanelTemplate PanelTemplate,
    BodyEsTemplate BodyEsTemplate,
    Customer Customer);
