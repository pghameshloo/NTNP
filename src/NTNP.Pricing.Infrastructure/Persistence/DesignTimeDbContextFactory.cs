using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NTNP.Pricing.Infrastructure.Persistence;

/// <summary>
/// Lets `dotnet ef migrations add/update` run against this class library directly (no Api project
/// needs to be the EF Core "startup project"). The connection string here is used only at
/// design-time to generate migration SQL; it is never used at runtime (see
/// deployment/database and docs/deployment.md for the real connection string configuration).
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        var connectionString = Environment.GetEnvironmentVariable("NTNP_DESIGN_TIME_CONNECTION_STRING")
            ?? "Server=(local);Database=NTNP_Pricing_Design;Trusted_Connection=True;TrustServerCertificate=True;";

        optionsBuilder.UseSqlServer(connectionString, sql => sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
