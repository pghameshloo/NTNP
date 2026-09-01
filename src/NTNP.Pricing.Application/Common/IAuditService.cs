using NTNP.Pricing.Domain.Enums;

namespace NTNP.Pricing.Application.Common;

/// <summary>Section 30 — writes one <see cref="Domain.Entities.AuditLogEntry"/> row per call.</summary>
public interface IAuditService
{
    Task LogAsync(
        AuditAction action,
        string entityType,
        string entityId,
        object? oldValue = null,
        object? newValue = null,
        string? reason = null,
        Guid? projectId = null,
        Guid? projectRevisionId = null,
        CancellationToken cancellationToken = default);
}
