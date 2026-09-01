using NTNP.Pricing.Domain.Enums;
using NTNP.Pricing.Domain.Exceptions;

namespace NTNP.Pricing.Domain.Calculation;

/// <summary>
/// Pure, side-effect-free implementations of every formula in Sections 9, 10, 11, 17, 18 and 19 of
/// the master prompt. Every method takes and returns <see cref="decimal"/> only (Section 4:
/// "never double or float"). No rounding is performed here — rounding is an explicit, separate,
/// output-stage concern (see <see cref="RoundingPolicy"/>).
/// </summary>
public static class PricingCalculationEngine
{
    /// <summary>
    /// Section 9:
    /// <code>
    /// If Purchase Currency = IRR:      Final Unit Cost IRR = Rial Unit Price
    /// If Purchase Currency != IRR:     Final Unit Cost IRR = Foreign Unit Price × Purchase Exchange Rate
    /// </code>
    /// </summary>
    public static decimal CalculateEquipmentFinalUnitCostIrr(
        string purchaseCurrencyCode,
        decimal? foreignUnitPrice,
        decimal? rialUnitPrice,
        decimal? purchaseExchangeRate)
    {
        var isIrr = string.Equals(purchaseCurrencyCode, "IRR", StringComparison.OrdinalIgnoreCase);

        if (isIrr)
        {
            if (foreignUnitPrice is > 0)
                throw new DomainValidationException("Foreign unit price must be empty or zero when purchase currency is IRR.");
            return rialUnitPrice ?? 0m;
        }

        if (foreignUnitPrice is null or <= 0)
            throw new DomainValidationException("Foreign unit price is required and must be positive for a non-IRR purchase currency.");
        if (purchaseExchangeRate is null or <= 0)
            throw new DomainValidationException("A valid (positive) purchase exchange rate is required for a non-IRR purchase currency.");

        return foreignUnitPrice.Value * purchaseExchangeRate.Value;
    }

    /// <summary>
    /// Sections 10/11 (identical formula shape for BOM and BODY+ES lines):
    /// <code>
    /// Adjusted Quantity = Quantity Per Panel × (1 + Waste Percentage)
    /// Line Cost IRR     = Adjusted Quantity × Unit Cost IRR
    /// </code>
    /// </summary>
    public static (decimal AdjustedQuantity, decimal LineCostIrr) CalculateLine(
        decimal quantityPerPanel, decimal wastePercentage, decimal unitCostIrr, decimal? costMultiplier = null)
    {
        if (quantityPerPanel < 0)
            throw new DomainValidationException("BOM/BODY+ES quantity per panel must not be negative.");
        if (wastePercentage < 0)
            throw new DomainValidationException("Waste percentage must not be negative.");

        var adjustedQuantity = quantityPerPanel * (1 + wastePercentage);
        var lineCost = adjustedQuantity * unitCostIrr * (costMultiplier ?? 1m);
        return (adjustedQuantity, lineCost);
    }

    /// <summary>Sections 10/11/17 — sum of line costs (Panel Equipment Cost / BODY+ES Cost Per Panel).</summary>
    public static decimal SumLineCosts(IEnumerable<decimal> lineCosts) => lineCosts.Sum();

    /// <summary>
    /// Section 17:
    /// <code>Total Cost Per Panel = Equipment Cost Per Panel + BODY+ES Cost Per Panel + Other Direct Cost Per Panel</code>
    /// </summary>
    public static decimal CalculateTotalCostPerPanel(decimal equipmentCostPerPanel, decimal bodyEsCostPerPanel, decimal otherDirectCostPerPanel)
        => equipmentCostPerPanel + bodyEsCostPerPanel + otherDirectCostPerPanel;

    /// <summary>Section 17: <code>Total Line Cost = Panel Quantity × Total Cost Per Panel</code></summary>
    public static decimal CalculateTotalLineCost(decimal panelQuantity, decimal totalCostPerPanel)
    {
        if (panelQuantity <= 0)
            throw new DomainValidationException("Panel quantity must be positive.");
        return panelQuantity * totalCostPerPanel;
    }

    /// <summary>
    /// Section 17 — markup vs. gross margin are never conflated:
    /// <code>
    /// Markup:       Selling Price Per Panel = Total Cost Per Panel × (1 + Markup)
    /// GrossMargin:  Selling Price Per Panel = Total Cost Per Panel ÷ (1 - GrossMargin)
    /// </code>
    /// </summary>
    public static decimal CalculateSellingPricePerPanel(decimal totalCostPerPanel, PricingMethod method, decimal rate)
    {
        return method switch
        {
            PricingMethod.Markup => totalCostPerPanel * (1 + rate),
            PricingMethod.GrossMargin => rate >= 1m
                ? throw new DomainValidationException("Gross margin must be below 100%.")
                : totalCostPerPanel / (1 - rate),
            _ => throw new DomainValidationException($"Unknown pricing method: {method}"),
        };
    }

    /// <summary>Section 17: <code>Total Line Selling Price = Panel Quantity × Selling Price Per Panel</code></summary>
    public static decimal CalculateTotalLineSellingPrice(decimal panelQuantity, decimal sellingPricePerPanel)
        => panelQuantity * sellingPricePerPanel;

    /// <summary>
    /// Section 18:
    /// <code>
    /// Rial Payable Amount           = Total Line Selling Price IRR × Rial Share
    /// Foreign Share Equivalent IRR  = Total Line Selling Price IRR × Foreign Share
    /// Foreign Payable Amount        = Foreign Share Equivalent IRR ÷ Quotation Currency Selling Rate
    /// </code>
    /// Validates <c>Rial Share + Foreign Share = 100%</c> first.
    /// </summary>
    public static (decimal RialPayableAmount, decimal ForeignShareEquivalentIrr, decimal ForeignPayableAmount) CalculateRialForeignSplit(
        decimal totalLineSellingPriceIrr, decimal rialShare, decimal foreignShare, decimal sellingExchangeRate)
    {
        ValidateShares(rialShare, foreignShare);

        if (sellingExchangeRate <= 0)
            throw new DomainValidationException("Selling exchange rate must be positive.");

        var rialPayable = totalLineSellingPriceIrr * rialShare;
        var foreignShareEquivalentIrr = totalLineSellingPriceIrr * foreignShare;
        var foreignPayable = foreignShareEquivalentIrr / sellingExchangeRate;

        return (rialPayable, foreignShareEquivalentIrr, foreignPayable);
    }

    /// <summary>Section 18/38: Rial Share + Foreign Share must equal exactly 100% (within a 1e-8 fraction tolerance for stored decimals).</summary>
    public static void ValidateShares(decimal rialShare, decimal foreignShare)
    {
        if (rialShare is < 0 or > 1)
            throw new DomainValidationException("Rial share must be between 0% and 100%.");
        if (foreignShare is < 0 or > 1)
            throw new DomainValidationException("Foreign share must be between 0% and 100%.");
        if (Math.Abs(rialShare + foreignShare - 1m) > 0.00000001m)
            throw new DomainValidationException("Rial share and foreign share must total 100%.");
    }

    /// <summary>
    /// Section 18:
    /// <code>
    /// Reconciliation Difference = Total Line Selling Price IRR - (Rial Payable Amount + Foreign Payable Amount × Selling Rate)
    /// </code>
    /// The difference must be (algebraically) zero before rounding; callers compare the returned
    /// value against a configured tolerance (Section 12) after any display rounding is applied.
    /// </summary>
    public static decimal CalculateReconciliationDifference(
        decimal totalLineSellingPriceIrr, decimal rialPayableAmount, decimal foreignPayableAmount, decimal sellingExchangeRate)
        => totalLineSellingPriceIrr - (rialPayableAmount + foreignPayableAmount * sellingExchangeRate);

    public static bool IsWithinTolerance(decimal difference, decimal toleranceIrr) => Math.Abs(difference) <= toleranceIrr;

    /// <summary>
    /// Section 17/19:
    /// <code>
    /// Profit       = Selling Price - Cost
    /// Gross Margin = Profit ÷ Selling Price
    /// </code>
    /// </summary>
    public static (decimal Profit, decimal GrossMargin) CalculateProfitAndMargin(decimal sellingPrice, decimal cost)
    {
        var profit = sellingPrice - cost;
        var grossMargin = sellingPrice == 0m ? 0m : profit / sellingPrice;
        return (profit, grossMargin);
    }
}
