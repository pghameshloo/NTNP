using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using NTNP.Pricing.Domain.Enums;

namespace NTNP.Pricing.Api.Authorization;

public static class AuthorizationSetup
{
    public static IServiceCollection AddNtnpAuthorization(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            // Section 6: every endpoint checks authorization server-side — anonymous access is
            // opt-in per endpoint ([AllowAnonymous] on login/refresh/health), never the default.
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
            .AddPolicy(PolicyNames.AdminOnly, p => p.RequireRole(Roles.Admin))
            .AddPolicy(PolicyNames.ManageUsers, p => p.RequireRole(Roles.Admin))
            .AddPolicy(PolicyNames.ManageCurrencies, p => p.RequireRole(Roles.Admin))
            .AddPolicy(PolicyNames.ManageEquipment, p => p.RequireRole(Roles.Admin))
            .AddPolicy(PolicyNames.ManageTemplates, p => p.RequireRole(Roles.Admin, Roles.Engineering))
            .AddPolicy(PolicyNames.ManageCustomers, p => p.RequireRole(Roles.Admin, Roles.Commercial))
            .AddPolicy(PolicyNames.ManageProjects, p => p.RequireRole(Roles.Admin, Roles.Commercial))
            .AddPolicy(PolicyNames.ManagePricingProfiles, p => p.RequireRole(Roles.Admin))
            .AddPolicy(PolicyNames.Approve, p => p.RequireRole(Roles.Admin, Roles.Approver))
            .AddPolicy(PolicyNames.ViewAuditLog, p => p.RequireRole(Roles.Admin))
            .AddPolicy(PolicyNames.ManageSettings, p => p.RequireRole(Roles.Admin))
            .AddPolicy(PolicyNames.ViewOnly, p => p.RequireRole(Roles.Admin, Roles.Engineering, Roles.Commercial, Roles.Approver, Roles.Viewer));

        return services;
    }
}
