using FluentValidation;
using NTNP.Pricing.Contracts.Currencies;
using NTNP.Pricing.Contracts.Customers;
using NTNP.Pricing.Contracts.Equipment;
using NTNP.Pricing.Contracts.PricingProfiles;
using NTNP.Pricing.Contracts.Projects;

namespace NTNP.Pricing.Application.Validators;

/// <summary>
/// Section 1/38 — FluentValidation rules for the highest-value request DTOs (structural rules that
/// don't need a database round-trip; uniqueness and other DB-backed checks stay in the Application
/// services, which run inside the same transaction as the write). Applied automatically to every
/// matching request by <c>NTNP.Pricing.Api</c>'s <c>ValidationActionFilter</c>. The WPF client also
/// runs the same class of checks client-side before submitting (see Desktop/Validation).
/// </summary>
public sealed class CreateCustomerRequestValidator : AbstractValidator<CreateCustomerRequest>
{
    public CreateCustomerRequestValidator()
    {
        RuleFor(x => x.CustomerCode).NotEmpty().MaximumLength(40);
        RuleFor(x => x.CompanyName).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}

public sealed class CreateCurrencyRequestValidator : AbstractValidator<CreateCurrencyRequest>
{
    public CreateCurrencyRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().Length(3).Matches("^[A-Za-z]{3}$").WithMessage("Currency code must be a 3-letter ISO 4217 code.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}

public sealed class CreateExchangeRateRequestValidator : AbstractValidator<CreateExchangeRateRequest>
{
    public CreateExchangeRateRequestValidator()
    {
        RuleFor(x => x.CurrencyId).NotEmpty();
        RuleFor(x => x.PurchaseRateToIrr).GreaterThan(0m).WithMessage("Purchase rate must be greater than zero (Section 8).");
        RuleFor(x => x.SellingRateToIrr).GreaterThan(0m).WithMessage("Selling rate must be greater than zero (Section 8).");
        RuleFor(x => x.EffectiveAtUtc).NotEmpty().WithMessage("Effective date is required (Section 8).");
    }
}

public sealed class CreateEquipmentRequestValidator : AbstractValidator<CreateEquipmentRequest>
{
    public CreateEquipmentRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(60);
        RuleFor(x => x.DescriptionFa).NotEmpty().MaximumLength(500);
        RuleFor(x => x.DescriptionEn).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Unit).NotEmpty().MaximumLength(20);
        RuleFor(x => x.LeadTimeDays).GreaterThanOrEqualTo(0).When(x => x.LeadTimeDays.HasValue);
    }
}

public sealed class CreateEquipmentPriceRequestValidator : AbstractValidator<CreateEquipmentPriceRequest>
{
    public CreateEquipmentPriceRequestValidator()
    {
        RuleFor(x => x.EquipmentId).NotEmpty();
        RuleFor(x => x.PurchaseCurrencyCode).NotEmpty().Length(3);
        RuleFor(x => x.ForeignUnitPrice).GreaterThan(0m)
            .When(x => !string.Equals(x.PurchaseCurrencyCode, "IRR", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Foreign unit price is required and must be positive for a non-IRR purchase currency (Section 9).");
        RuleFor(x => x.ForeignUnitPrice).Empty()
            .When(x => string.Equals(x.PurchaseCurrencyCode, "IRR", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Foreign unit price must be empty when purchase currency is IRR (Section 9).");
        RuleFor(x => x.RialUnitPrice).GreaterThan(0m)
            .When(x => string.Equals(x.PurchaseCurrencyCode, "IRR", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Rial unit price is required and must be positive for IRR purchase currency (Section 9).");
    }
}

public sealed class UpsertPricingProfileRequestValidator : AbstractValidator<UpsertPricingProfileRequest>
{
    public UpsertPricingProfileRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.PricingMethod).Must(m => m is "Markup" or "GrossMargin").WithMessage("Pricing method must be 'Markup' or 'GrossMargin' (Section 12).");
        RuleFor(x => x.DefaultRate).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.DefaultRate).LessThan(1m).When(x => x.PricingMethod == "GrossMargin")
            .WithMessage("Gross margin must be below 100% (Sections 17/38).");
        RuleFor(x => x.DefaultRialShare).InclusiveBetween(0m, 1m);
        RuleFor(x => x.DefaultForeignShare).InclusiveBetween(0m, 1m);
        RuleFor(x => x).Must(x => Math.Abs(x.DefaultRialShare + x.DefaultForeignShare - 1m) < 0.00000001m)
            .WithMessage("Rial share and foreign share must total 100% (Sections 18/38).");
        RuleFor(x => x.DefaultQuotationCurrencyCode).NotEmpty().Length(3);
        RuleFor(x => x.ReconciliationToleranceIrr).GreaterThanOrEqualTo(0m);
    }
}

public sealed class CreateProjectRequestValidator : AbstractValidator<CreateProjectRequest>
{
    public CreateProjectRequestValidator()
    {
        RuleFor(x => x.ProjectCode).NotEmpty().MaximumLength(60);
        RuleFor(x => x.ProjectName).NotEmpty().MaximumLength(300);
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.QuotationCurrencyCode).NotEmpty().Length(3);
        RuleFor(x => x.RialShare).InclusiveBetween(0m, 1m);
        RuleFor(x => x.ForeignShare).InclusiveBetween(0m, 1m);
        RuleFor(x => x).Must(x => Math.Abs(x.RialShare + x.ForeignShare - 1m) < 0.00000001m)
            .WithMessage("Rial share and foreign share must total 100% (Sections 18/38).");
        RuleFor(x => x.PricingMethod).Must(m => m is "Markup" or "GrossMargin");
        RuleFor(x => x.PricingRate).GreaterThanOrEqualTo(0m);
    }
}

public sealed class AddProjectLineRequestValidator : AbstractValidator<AddProjectLineRequest>
{
    public AddProjectLineRequestValidator()
    {
        RuleFor(x => x.PanelTemplateId).NotEmpty();
        RuleFor(x => x.CellCode).NotEmpty().MaximumLength(60);
        RuleFor(x => x.QuantityOfPanels).GreaterThan(0m).WithMessage("Panel quantity must be positive (Section 38).");
        RuleFor(x => x.OtherDirectCostPerPanel).GreaterThanOrEqualTo(0m);
    }
}
