using Microsoft.EntityFrameworkCore;
using NTNP.Pricing.Application.Common;
using NTNP.Pricing.Domain.Enums;
using NTNP.Pricing.Infrastructure.Persistence;

namespace NTNP.Pricing.Application.Tests.TestDoubles;

public sealed class FakeCurrentUserService : ICurrentUserService
{
    public Guid UserId { get; set; } = Guid.NewGuid();
    public string UserName { get; set; } = "test.user";
    public IReadOnlyList<string> Roles { get; set; } = new[] { Domain.Enums.Roles.Admin };
    public bool IsInRole(string role) => Roles.Contains(role);
    public string? IpAddress => "127.0.0.1";
}

public sealed class FakeDateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow { get; set; } = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
}

public sealed class NoOpAuditService : IAuditService
{
    public Task LogAsync(AuditAction action, string entityType, string entityId, object? oldValue = null, object? newValue = null,
        string? reason = null, Guid? projectId = null, Guid? projectRevisionId = null, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

/// <summary>Creates an isolated, EF Core InMemory-backed <see cref="ApplicationDbContext"/> per test.</summary>
public static class TestDbContextFactory
{
    public static ApplicationDbContext Create()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"ntnp-app-tests-{Guid.NewGuid():N}")
            .Options;
        var db = new ApplicationDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }
}
