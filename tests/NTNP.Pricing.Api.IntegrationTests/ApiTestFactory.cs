using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NTNP.Pricing.Infrastructure.Persistence;

namespace NTNP.Pricing.Api.IntegrationTests;

/// <summary>
/// Boots the real Api pipeline — controllers, JWT auth, RBAC policies, FluentValidation filter,
/// exception middleware, DbSeeder — end-to-end over an in-process <c>TestServer</c>, with only the
/// SQL Server connection swapped for an isolated EF Core InMemory database per factory instance
/// (see the csproj comment for why InMemory, not SQLite). Everything else — Program.cs's
/// AddInfrastructure/AddApplication/AddReporting/AddNtnpAuthorization wiring, the JWT bearer
/// pipeline, the seeded reference-scenario project (Section 20) — runs unmodified.
/// </summary>
public sealed class ApiTestFactory : WebApplicationFactory<Program>
{
    // Program.cs reads ConnectionStrings:SqlServer and Jwt:SigningKey/Issuer/Audience directly off
    // WebApplicationBuilder.Configuration BEFORE builder.Build() is called (the connection string
    // inside AddInfrastructure(), the Jwt values while configuring AddJwtBearer()) — i.e. before
    // WebApplicationFactory's ConfigureWebHost/ConfigureAppConfiguration hook has a chance to layer
    // its overrides in (that hook applies at Build() time). A real process environment variable,
    // set before the host is ever created, is the only override those particular reads will see;
    // everything else in Program.cs (FileStorage, Database:AutoMigrate/Seed) is read lazily enough
    // that ConfigureAppConfiguration below reaches it in time.
    static ApiTestFactory()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__SqlServer", "Server=unused;Database=unused;Trusted_Connection=True;TrustServerCertificate=True;");
        Environment.SetEnvironmentVariable("Jwt__SigningKey", "integration-test-only-signing-key-at-least-32-characters-long");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "NTNP.Pricing.Tests");
        Environment.SetEnvironmentVariable("Jwt__Audience", "NTNP.Pricing.Tests.Clients");
    }

    public string DbName { get; } = $"ntnp-api-tests-{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FileStorage:RootPath"] = Path.Combine(Path.GetTempPath(), "ntnp-api-tests-storage", DbName),
                ["Database:AutoMigrate"] = "false", // InMemory has no migrations to apply
                ["Database:Seed"] = "true", // exercises the real Section 37 seeder, incl. the Section 20 reference project
                ["Serilog:WriteTo:1:Args:path"] = Path.Combine(Path.GetTempPath(), "ntnp-api-tests-logs", "api-tests-.log"),
            });
        });

        builder.ConfigureServices(services =>
        {
            // AddInfrastructure() already called AddDbContext<ApplicationDbContext>(UseSqlServer).
            // Modern EF Core registers that provider configuration as its own singleton descriptors
            // (DbContextOptions<ApplicationDbContext> plus one IDbContextOptionsConfiguration<T> per
            // AddDbContext call) rather than one monolithic delegate — removing only
            // DbContextOptions<ApplicationDbContext> leaves the SqlServer IDbContextOptionsConfiguration
            // entry behind, so a second AddDbContext call for InMemory ends up layered on top of it and
            // EF Core refuses to build ("two database providers registered"). Remove every descriptor
            // that closes over ApplicationDbContext before re-registering.
            var contextDescriptors = services
                .Where(d => d.ServiceType.IsGenericType && d.ServiceType.GetGenericArguments().Contains(typeof(ApplicationDbContext)))
                .ToList();
            foreach (var descriptor in contextDescriptors)
                services.Remove(descriptor);

            services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(DbName));
        });
    }
}
