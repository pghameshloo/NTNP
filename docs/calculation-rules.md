# Calculation Rules

Every formula below is implemented in `src/NTNP.Pricing.Domain/Calculation/` as a pure, static
method taking/returning `decimal` (never `double`/`float` — Section "Financial Precision") and is
covered by `tests/NTNP.Pricing.Domain.Tests/ReferenceCalculationScenarioTests.cs`, the mandatory
Section 20 scenario. IRR is the canonical internal currency; every stored figure is IRR unless
explicitly named otherwise.

## 1. Equipment Final Unit Cost (Section 9)

`PricingCalculationEngine.CalculateEquipmentFinalUnitCostIrr`

```text
If Purchase Currency = IRR:   Final Unit Cost IRR = Rial Unit Price
If Purchase Currency != IRR:  Final Unit Cost IRR = Foreign Unit Price × Purchase Exchange Rate
```

Enforced: IRR equipment must not carry a foreign price; foreign-currency equipment requires both a
foreign price and a purchase exchange rate. A master-data price update never touches an already
snapshotted `ProjectLineBomItem.UnitCostIrrSnapshot` (Section 9 "Updating a master price must not
alter approved project revisions").

## 2. Equipment Cost Per Panel (Section 17)

`PricingCalculationEngine.SumLineCosts` over `ProjectLineCalculator.CalculateBomItem`

```text
Equipment Cost Per Panel = SUM(BOM Quantity Per Panel × Waste-adjusted × Equipment Final Unit Cost IRR)
```

Each BOM line's `AdjustedQuantityPerPanel = QuantityPerPanel × (1 + WastePercentage)` before being
multiplied by the snapshotted unit cost.

## 3. BODY+ES Cost Per Panel (Section 11)

Calculated the same way, independently, from `BodyEsTemplateItem`s, then flows into Total Cost Per
Panel as its own line item — never merged into Equipment Cost Per Panel (Section 40 "BODY+ES
calculates separately and flows into panel cost").

## 4. Total Cost Per Panel (Section 17)

```text
Total Cost Per Panel = Equipment Cost Per Panel + BODY+ES Cost Per Panel + Other Direct Cost Per Panel
```

## 5. Total Line Cost (Section 17)

```text
Total Line Cost = Panel Quantity × Total Cost Per Panel
```

## 6. Selling Price Per Panel — Markup vs. Gross Margin (Section 17)

These two methods are **never conflated** — `PricingMethod` is an explicit enum
(`Markup`/`GrossMargin`), not inferred from the rate's magnitude:

```text
Markup:        Selling Price Per Panel = Total Cost Per Panel × (1 + Markup Rate)
                 (a rate of 0.30 means "30% markup", multiplier 1.30)

Gross Margin:  Selling Price Per Panel = Total Cost Per Panel ÷ (1 - Gross Margin Rate)
                 (30% gross margin → multiplier 1 ÷ 0.70 = 1.428571...)
```

## 7. Total Line Selling Price (Section 17)

```text
Total Line Selling Price = Panel Quantity × Selling Price Per Panel
```

## 8. Rial / Foreign-Currency Split and Reconciliation (Section 18)

Validated: `Rial Share + Foreign Share = 100%` (`PricingCalculationEngine.ValidateShares`).

```text
Rial Payable Amount            = Total Line Selling Price IRR × Rial Share
Foreign Share Equivalent IRR   = Total Line Selling Price IRR × Foreign Share
Foreign Payable Amount         = Foreign Share Equivalent IRR ÷ Quotation Currency Selling Rate

Reconciliation Difference      = Total Line Selling Price IRR
                                  − (Rial Payable Amount + Foreign Payable Amount × Selling Rate)
```

The division in Foreign Payable Amount does not generally terminate in decimal (e.g.
1,860.083333...) — `IsWithinTolerance` compares the reconciliation difference against the pricing
profile's configured tolerance (default 1 IRR, `ASSUMPTIONS.md` §6) rather than requiring an exact
zero. Approval is blocked while reconciliation fails (Section 19).

## 9. TOTAL / Project Summary (Section 19)

```text
Total Equipment Cost       = SUM(line Equipment Cost Per Panel × Panel Quantity)
Total BODY+ES Cost         = SUM(line BODY+ES Cost Per Panel × Panel Quantity)
Total Other Direct Cost    = SUM(line Other Direct Cost Per Panel × Panel Quantity)
Total Project Cost         = SUM(Total Line Cost)
Total Project Selling Price = SUM(Total Line Selling Price)
Total Rial Payable         = SUM(Rial Payable Amount)
Total Foreign Payable      = SUM(Foreign Payable Amount)
Project Profit             = Total Project Selling Price − Total Project Cost
Project Gross Margin       = Project Profit ÷ Total Project Selling Price
```

`ProjectTotalsCalculator.GetApprovalBlockers` returns a non-empty list — and blocks submission —
when any of: exchange rate missing/zero, shares ≠ 100%, a required equipment price is missing, a BOM
or BODY+ES quantity is invalid, reconciliation fails, gross margin ≥ 100% (or otherwise invalid), or
any line carries validation errors.

## 10. Automatic Consolidated MTO (Section 16)

`MtoCalculator.CalculateElectricalMto` / `CalculateBodyEsMto` / `CalculateCombinedMto`, grouped by
Equipment/Component Code across every line in the revision:

```text
Required Equipment Quantity = SUM(BOM Quantity Per Panel × Number of Panels) across all lines using that code
Total Procurement Cost      = Required Equipment Quantity × Snapshot Unit Cost IRR
```

Three views are generated from the same underlying data: Electrical-only, BODY+ES-only, and
Combined — never three independently-maintained calculations.

## 11. Mandatory Reference Scenario (Section 20)

The exact numbers below are asserted verbatim (to the decimal, not rounded) by
`ReferenceCalculationScenarioTests` and reproduced by the seeded sample project `PRJ-0001`
(`SampleProjectSeeder`), and re-verified end to end over real HTTP in
`Api.IntegrationTests/ProjectCalculationFlowTests`:

| Input | Value |
|---|---|
| EUR purchase rate | 1,800,000 IRR |
| EUR selling rate | 1,800,000 IRR |
| Foreign share / Rial share | 85% / 15% |
| Pricing method | Markup, 30% (multiplier 1.30) |
| Air Circuit Breaker | qty 2, 800 EUR each |
| Relay | qty 3, 50,000,000 IRR each |

| Result | Value |
|---|---|
| ACB unit cost | 800 × 1,800,000 = **1,440,000,000 IRR** |
| ACB line cost | 2 × 1,440,000,000 = **2,880,000,000 IRR** |
| Relay line cost | 3 × 50,000,000 = **150,000,000 IRR** |
| Total project cost | **3,030,000,000 IRR** |
| Total selling price | 3,030,000,000 × 1.30 = **3,939,000,000 IRR** |
| Rial payable | 3,939,000,000 × 15% = **590,850,000 IRR** |
| EUR payable | (3,939,000,000 × 85%) ÷ 1,800,000 = **1,860.083333... EUR** |
| Profit | 3,939,000,000 − 3,030,000,000 = **909,000,000 IRR** |
| Gross margin | 909,000,000 ÷ 3,939,000,000 = **23.076923...%** |
| Reconciliation | 590,850,000 + (1,860.083333... × 1,800,000) = **3,939,000,000 IRR** (exact) |

See the final delivery summary for the actual `dotnet test` results confirming every one of these.
