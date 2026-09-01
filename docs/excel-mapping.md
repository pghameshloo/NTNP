# Excel → NTNP Pricing Engine Mapping

> **Note on source material:** no `Pricing-table.xlsx` workbook was present in this session's
> attachments (see `ASSUMPTIONS.md` §0). This mapping is built from the legacy process as narratively
> and formulaically specified in the master prompt itself. When the real workbook is supplied, re-run
> the Equipment Database Excel import wizard against it (column mapping is data-driven, no code
> change required) and diff this document against the actual sheet/column names.

## 1. Workbook → Module map

| Legacy Excel sheet/concept | New system module | Notes |
|---|---|---|
| `SOURCE PRICE DEVICES` | **Equipment Database** (`Equipment`, `EquipmentPrice`) | Section 9. Equipment Code becomes the unique lookup key; every price change creates a new `EquipmentPrice` row (history), never overwrites. |
| Panel-type sheets (`INCOMING`, `OUTGOING`, `BUS COUPLER`, …) | **Panel Templates + BOM** (`PanelTemplate`, `PanelTemplateBomItem`) | Section 10. One workbook sheet per panel type becomes one versioned `PanelTemplate` row per (Product Family, Voltage Level, Panel Type) combination; BOM rows become `PanelTemplateBomItem` rows referencing `Equipment` by code (never a copied price). |
| `BODY+ES` sheet | **BODY+ES Templates** (`BodyEsTemplate`, `BodyEsTemplateItem`) | Section 11. Kept as its own template/costing pipeline, summed into panel cost but reported separately. |
| `TOTAL` sheet | **Project Lineup + TOTAL screen** (`ProjectLine`, `ProjectRevisionTotal`) | Section 19. No manual price entry is permitted here in the new system — it is a pure read model computed from the BOM/BODY+ES/pricing engine. |
| Project quotation cells (Rial/foreign split, markup) | **Pricing Profiles + Project Pricing Settings** | Section 12/17/18. Markup vs. gross-margin, Rial/foreign share and quotation currency are now explicit, auditable, versioned settings instead of ad-hoc formulas per workbook copy. |
| Manual currency conversion cells | **Currency & Exchange Rate module** | Section 8. Purchase rate and selling rate are separated; every project revision snapshots the rate used. |

## 2. Field mapping — Equipment

| Excel column (typical) | New field | Entity.Property |
|---|---|---|
| Technical No. / Equipment Code | Equipment Code (unique) | `Equipment.Code` |
| Part Number | Technical Part Number | `Equipment.PartNumber` |
| شرح فارسی | Persian Description | `Equipment.DescriptionFa` |
| Description (EN) | English Description | `Equipment.DescriptionEn` |
| Group/Category | Category / Subcategory | `Equipment.CategoryId` / `Equipment.SubcategoryId` |
| Brand | Brand | `Equipment.Brand` |
| Model | Model | `Equipment.Model` |
| Manufacturer | Manufacturer | `Equipment.Manufacturer` |
| Supplier | Supplier | `Equipment.Supplier` |
| Currency | Purchase Currency | `EquipmentPrice.PurchaseCurrencyCode` |
| Foreign Unit Price | Foreign Unit Price | `EquipmentPrice.ForeignUnitPrice` |
| Rial Unit Price | Rial Unit Price | `EquipmentPrice.RialUnitPrice` |
| (rate used at entry time) | Applicable Purchase Exchange Rate | `EquipmentPrice.PurchaseExchangeRateSnapshot` |
| (computed cell) | Final Unit Cost IRR | `EquipmentPrice.FinalUnitCostIrr` (computed, see calculation-rules.md §1) |

## 3. Field mapping — Panel BOM line

| Excel column | New field | Entity.Property |
|---|---|---|
| Item code (lookup into SOURCE PRICE DEVICES) | Equipment reference | `PanelTemplateBomItem.EquipmentId` (FK, resolved by `Equipment.Code`) |
| Qty | Quantity Per Panel | `PanelTemplateBomItem.QuantityPerPanel` |
| Unit | Unit | `PanelTemplateBomItem.Unit` |
| Waste %/scrap allowance (if present) | Waste Percentage | `PanelTemplateBomItem.WastePercentage` |
| (Excel `=Qty*UnitPrice`) | BOM Line Cost IRR | computed, see calculation-rules.md §2 |
| (Excel `=SUM(...)`) | Panel Equipment Cost IRR | computed, see calculation-rules.md §2 |

## 4. Field mapping — BODY+ES

| Excel column | New field | Entity.Property |
|---|---|---|
| Component description | Component Description | `BodyEsTemplateItem.DescriptionFa`/`En` |
| Qty per panel | Quantity Per Panel | `BodyEsTemplateItem.QuantityPerPanel` |
| Waste % | Waste Percentage | `BodyEsTemplateItem.WastePercentage` |
| Unit cost | Unit Cost IRR | `BodyEsTemplateItem.UnitCostIrr` |
| (Excel row total) | Line Cost | computed |
| (Excel column total) | BODY+ES Cost Per Panel | computed, see calculation-rules.md §3 |

## 5. Field mapping — TOTAL

Every `TOTAL` sheet column in Section 19 is reproduced 1:1 as a `ProjectLine` read-model column
(see `NTNP.Pricing.Contracts/Projects/ProjectTotalLineDto.cs`) computed server-side by
`ITotalCalculationService`; none of them are editable cells in the new system except the explicitly
authorized-override fields (Section 14), which require reason + audit entry.

## 6. What changed structurally vs. the Excel process

1. **No manual price re-entry per sheet.** All prices are looked up once from the Equipment
   Database by Equipment Code and are versioned (`EquipmentPrice` history), eliminating the
   "stale copy-pasted price" failure mode of the workbook.
2. **No cross-sheet cell references.** Panel templates, BODY+ES templates and TOTAL never reference
   another workbook's cell; they reference normalized rows by foreign key, and a project revision
   stores an **immutable snapshot** of every price/rate/template version it used (Section 13).
3. **Markup vs. gross margin are distinct, named pricing methods** (Section 17) instead of an
   implicit multiplier baked into a TOTAL formula — this is the most common source of NTNP
   pricing errors in the legacy sheet and is now enforced by validation (`GrossMargin < 100%`,
   explicit `PricingMethod` enum).
4. **Purchase and selling exchange rates are independent** fields with independent history, whereas
   the workbook typically used one "current rate" cell for both purposes.
5. **Reconciliation is computed and blocking**, not a visual cross-check left to the estimator.

See `docs/calculation-rules.md` for the exact formulas implemented (verbatim from Sections 17–20 of
the master prompt) and `tests/NTNP.Pricing.Domain.Tests/ReferenceCalculationScenarioTests.cs` for the
executable proof against the mandatory worked example.
