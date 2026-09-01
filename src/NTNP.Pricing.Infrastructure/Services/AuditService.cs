using System.Text.Json;
using NTNP.Pricing.Application.Common;
using NTNP.Pricing.Domain.Entities;
using NTNP.Pricing.Domain.Enums;
using NTNP.Pricing.Infrastructure.Persistence;

namespace NTNP.Pricing.Infrastructure.Services;

/// <summary>
/// Section 30. Redacts any object property whose name looks secret-like (password/token/secret/
/// connectionstring/hash) before serializing old/new values, so a caller can never accidentally
/// leak a credential into the audit trail even via a raw entity dump.
/// </summary>
public sealed class AuditService : IAuditService
{
    private static readonly string[] SecretLikeNames = { "password", "token", "secret", "connectionstring", "hash", "salt" };

    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public AuditService(ApplicationDbContext db, ICurrentUserService currentUser, IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task LogAsync(
        AuditAction action, string entityType, string entityId, object? oldValue = null, object? newValue = null,
        string? reason = null, Guid? projectId = null, Guid? projectRevisionId = null, CancellationToken cancellationToken = default)
    {
        _db.AuditLogEntries.Add(new AuditLogEntry
        {
            UserId = _currentUser.UserId,
            UserName = _currentUser.UserName,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            OldValueJson = Redact(oldValue),
            NewValueJson = Redact(newValue),
            AtUtc = _clock.UtcNow,
            Reason = reason,
            ProjectId = projectId,
            ProjectRevisionId = projectRevisionId,
            IpAddress = _currentUser.IpAddress,
        });

        await _db.SaveChangesAsync(cancellationToken);
    }

    private static string? Redact(object? value)
    {
        if (value is null) return null;

        var json = JsonSerializer.Serialize(value);
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Object) return json;

        var redacted = new Dictionary<string, object?>();
        foreach (var prop in document.RootElement.EnumerateObject())
        {
            var isSecret = SecretLikeNames.Any(s => prop.Name.Contains(s, StringComparison.OrdinalIgnoreCase));
            redacted[prop.Name] = isSecret ? "***REDACTED***" : JsonSerializer.Deserialize<object?>(prop.Value.GetRawText());
        }
        return JsonSerializer.Serialize(redacted);
    }
}
