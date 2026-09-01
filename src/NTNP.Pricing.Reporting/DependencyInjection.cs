using Microsoft.Extensions.DependencyInjection;
using NTNP.Pricing.Reporting.Rendering;

namespace NTNP.Pricing.Reporting;

public static class DependencyInjection
{
    public static IServiceCollection AddReporting(this IServiceCollection services)
    {
        services.AddSingleton<HtmlToPdfRenderer>(); // owns one long-lived Chromium instance
        services.AddSingleton<IReportRenderer, ReportRenderer>();
        return services;
    }
}
