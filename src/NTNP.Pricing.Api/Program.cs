using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NTNP.Pricing.Api.Authorization;
using NTNP.Pricing.Api.Middleware;
using NTNP.Pricing.Api.Services;
using NTNP.Pricing.Application;
using NTNP.Pricing.Application.Common;
using NTNP.Pricing.Infrastructure;
using NTNP.Pricing.Infrastructure.Auth;
using NTNP.Pricing.Infrastructure.Persistence;
using NTNP.Pricing.Infrastructure.Seed;
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

    if (builder.Configuration.GetValue<bool>("Database:AutoMigrate"))
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();
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
