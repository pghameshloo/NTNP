using Microsoft.Extensions.DependencyInjection;
using NTNP.Pricing.Application.Audit;
using NTNP.Pricing.Application.BodyEs;
using NTNP.Pricing.Application.Currencies;
using NTNP.Pricing.Application.Customers;
using NTNP.Pricing.Application.Dashboard;
using NTNP.Pricing.Application.Equipment;
using NTNP.Pricing.Application.PanelTemplates;
using NTNP.Pricing.Application.PricingProfiles;
using NTNP.Pricing.Application.Projects;
using NTNP.Pricing.Application.Settings;

namespace NTNP.Pricing.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<ICurrencyService, CurrencyService>();
        services.AddScoped<IEquipmentService, EquipmentService>();
        services.AddScoped<ILookupService, LookupService>();
        services.AddScoped<IPanelTemplateService, PanelTemplateService>();
        services.AddScoped<IBodyEsTemplateService, BodyEsTemplateService>();
        services.AddScoped<IPricingProfileService, PricingProfileService>();
        services.AddScoped<BomSnapshotBuilder>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IProjectLineService, ProjectLineService>();
        services.AddScoped<IMtoService, MtoService>();
        services.AddScoped<IProjectRevisionService, ProjectRevisionService>();
        services.AddScoped<IAuditQueryService, AuditQueryService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<ICompanySettingsService, CompanySettingsService>();

        // IEquipmentImportService, IUserManagementService and IAuthService are registered by
        // NTNP.Pricing.Infrastructure.DependencyInjection (they depend on ASP.NET Core Identity /
        // ClosedXML, which Application does not reference).

        return services;
    }
}
