using NTNP.Pricing.Domain.Common;
using NTNP.Pricing.Domain.Enums;

namespace NTNP.Pricing.Domain.Entities;

/// <summary>
/// Section 30 — one append-only audit record. Never stores plain-text passwords, tokens, database
/// credentials or connection strings (enforced by the Application-layer audit writer, which
/// redacts any field name matching a secret-like pattern before this row is created).
/// </summary>
public class AuditLogEntry : Entity
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public AuditAction Action { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string? OldValueJson { get; set; }
    public string? NewValueJson { get; set; }
    public DateTimeOffset AtUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? Reason { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? ProjectRevisionId { get; set; }
    public string? IpAddress { get; set; }
}
