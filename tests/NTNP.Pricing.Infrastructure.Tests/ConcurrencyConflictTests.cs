using Microsoft.EntityFrameworkCore;
using NTNP.Pricing.Domain.Entities;
using NTNP.Pricing.Infrastructure.Persistence;

namespace NTNP.Pricing.Infrastructure.Tests;

/// <summary>Section 31 — RowVersion-based optimistic concurrency must be detected, never silently overwritten.</summary>
public class ConcurrencyConflictTests
{
    [Fact]
    public async Task Concurrent_Edit_To_Same_Customer_Throws_DbUpdateConcurrencyException()
    {
        var dbName = $"ntnp-concurrency-{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(dbName).Options;

        await using (var seedDb = new ApplicationDbContext(options))
        {
            await seedDb.Database.EnsureCreatedAsync();
            seedDb.Customers.Add(new Customer
            {
                CustomerCode = "C-001",
                CompanyName = "Original Co.",
                CreatedByUserId = Guid.NewGuid(),
                CreatedByUserName = "tester",
            });
            await seedDb.SaveChangesAsync();
        }

        // Two independent contexts loading the same row, simulating two concurrent users.
        await using var contextA = new ApplicationDbContext(options);
        await using var contextB = new ApplicationDbContext(options);

        var customerA = await contextA.Customers.SingleAsync(c => c.CustomerCode == "C-001");
        var customerB = await contextB.Customers.SingleAsync(c => c.CustomerCode == "C-001");

        customerA.CompanyName = "Updated By User A";
        await contextA.SaveChangesAsync();

        customerB.CompanyName = "Updated By User B (stale)";

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => contextB.SaveChangesAsync());
    }
}
