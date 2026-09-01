using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NTNP.Pricing.Infrastructure.Identity;
using NTNP.Pricing.Infrastructure.Persistence;
using NTNP.Pricing.Infrastructure.Seed;

namespace NTNP.Pricing.Infrastructure.Tests;

/// <summary>
/// Builds a full DI container (Identity + EF Core over an EF Core InMemory database) and runs
/// <see cref="DbSeeder"/>. InMemory (rather than SQLite) is used here specifically because it
/// natively emulates <c>ValueGenerated.OnAddOrUpdate</c> concurrency tokens (RowVersion) the same
/// way SQL Server's `rowversion` does; SQLite requires trigger SQL that only real migrations emit.
/// The actual SQL Server migration SQL is verified separately (see
/// <c>NTNP.Pricing.Infrastructure/Persistence/Migrations</c> and docs/deployment.md).
/// </summary>
public sealed class SeededServiceProviderFixture : IAsyncLifetime
{
    private readonly string _dbName = $"ntnp-seed-{Guid.NewGuid():N}";
    public ServiceProvider Provider { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(o => o.UseInMemoryDatabase(_dbName));
        services.AddLogging();

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = true;
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

        Provider = services.BuildServiceProvider();

        using var scope = Provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.EnsureCreatedAsync();

        await DbSeeder.SeedAsync(Provider);
    }

    public Task DisposeAsync()
    {
        Provider.Dispose();
        return Task.CompletedTask;
    }
}
