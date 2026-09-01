using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NTNP.Pricing.Contracts.Common;
using NTNP.Pricing.Infrastructure.Persistence;

namespace NTNP.Pricing.Api.Controllers;

/// <summary>
/// Section 33 — the desktop client's "Server Connection Settings" screen calls this (unauthenticated,
/// on the internal LAN only) for Test Connection / server availability / API version / DB schema version.
/// </summary>
[ApiController]
[Route("api/health")]
[AllowAnonymous]
public sealed class HealthController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public HealthController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<ServerStatusDto>> Get(CancellationToken ct)
    {
        var apiVersion = typeof(HealthController).Assembly.GetName().Version?.ToString() ?? "1.0.0";
        bool dbReachable;
        string schemaVersion;
        try
        {
            var lastMigration = await _db.Database.GetAppliedMigrationsAsync(ct);
            schemaVersion = lastMigration.LastOrDefault() ?? "unmigrated";
            dbReachable = true;
        }
        catch
        {
            dbReachable = false;
            schemaVersion = "unknown";
        }

        return Ok(new ServerStatusDto(apiVersion, schemaVersion, dbReachable, DateTimeOffset.UtcNow));
    }
}
