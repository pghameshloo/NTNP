using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NTNP.Pricing.Api.Authorization;
using NTNP.Pricing.Api.Middleware;
using NTNP.Pricing.Api.Services;
using NTNP.Pricing.Api.Tools;
using NTNP.Pricing.Application;
using NTNP.Pricing.Application.Common;
using NTNP.Pricing.Infrastructure;
using NTNP.Pricing.Infrastructure.Auth;
using NTNP.Pricing.Infrastructure.Persistence;
using NTNP.Pricing.Infrastructure.Seed;
using NTNP.Pricing.Reporting;
using Serilog;

// Section 5/34: hosted as a Windows Service in production (see deployment/server), runs standalone
// via `dotnet run` / Kestrel for local development. Section 1: OpenAPI only for internal
// development/administration — never exposed in a production build.

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseWindowsService(options => options.ServiceName = "NTNP Pricing Engine Service");
    builder.Host.UseSerilog((context, services, configuration) =>
        configuration.ReadFrom.Configuration(context.Configuration).ReadFrom.Services(services));

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICurrentUserService, HttpCurrentUserService>();

    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddApplication();
    builder.Services.AddReporting();
    builder.Services.AddNtnpAuthorization();

    var jwtSection = builder.Configuration.GetSection("Jwt");
    var signingKey = jwtSection["SigningKey"];
    if (string.IsNullOrWhiteSpace(signingKey))
        throw new InvalidOperationException("Jwt:SigningKey is not configured. Set it via appsettings, an environment variable, or a deployment secret store (never commit a production key).");

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtSection["Issuer"],
                ValidateAudience = true,
                ValidAudience = jwtSection["Audience"],
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30),
            };
        });

    builder.Services.AddControllers(options => options.Filters.Add<ValidationActionFilter>());
    builder.Services.AddOpenApi();

    var app = builder.Build();

    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi().AllowAnonymous(); // internal dev/admin convenience only — never enabled in production
    }

    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    // Section 35 "SQL Server migration utility": `NTNP.Pricing.Api.exe migrate` applies pending EF
    // Core migrations and exits — a clean, auditable, non-interactive step an admin runs
    // deliberately (see deployment/database/migrate.ps1), distinct from Database:AutoMigrate below
    // (which migrates automatically on every service start — useful for dev, deliberately off by
    // default in production so schema changes are a reviewed, explicit action).
    if (args.Length > 0 && string.Equals(args[0], "migrate", StringComparison.OrdinalIgnoreCase))
    {
        using var migrateScope = app.Services.CreateScope();
        var migrateDb = migrateScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var pending = (await migrateDb.Database.GetPendingMigrationsAsync()).ToList();
        if (pending.Count == 0)
        {
            Console.WriteLine("Database is already up to date — no pending migrations.");
            return 0;
        }
        Console.WriteLine($"Applying {pending.Count} pending migration(s): {string.Join(", ", pending)}");
        await migrateDb.Database.MigrateAsync();
        Console.WriteLine("Migration complete.");
        return 0;
    }

    if (builder.Configuration.GetValue<bool>("Database:AutoMigrate"))
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();
    }

    // Section 35 "Initial Admin creation utility": `NTNP.Pricing.Api.exe create-admin [...]`
    // creates the first production Admin account through the real Identity stack, then exits —
    // it never starts Kestrel/the Windows Service. See deployment/database/create-admin.ps1 and
    // docs/deployment.md. Checked after AutoMigrate above (so the Users table exists) but before
    // the dev-only DbSeeder call below (which must never run against a production database).
    if (args.Length > 0 && string.Equals(args[0], "create-admin", StringComparison.OrdinalIgnoreCase))
    {
        return await AdminBootstrap.RunAsync(app.Services, args);
    }

    if (builder.Configuration.GetValue<bool>("Database:Seed"))
    {
        await DbSeeder.SeedAsync(app.Services);
    }

    Log.Information("NTNP Pricing Engine API starting up");
    app.Run();
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "NTNP Pricing Engine API terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

namespace NTNP.Pricing.Api
{
    /// <summary>Exposed for <c>WebApplicationFactory&lt;Program&gt;</c> in integration tests.</summary>
    public partial class Program;
}
