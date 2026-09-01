namespace NTNP.Pricing.Contracts.Audit;

public sealed record AuditLogEntryDto(
    Guid Id, string UserName, string Action, string EntityType, string EntityId,
    string? OldValueJson, string? NewValueJson, DateTimeOffset AtUtc, string? Reason,
    Guid? ProjectId, Guid? ProjectRevisionId);

public sealed record AuditLogQuery(
    string? EntityType = null, string? EntityId = null, Guid? UserId = null, Guid? ProjectId = null,
    DateTimeOffset? FromUtc = null, DateTimeOffset? ToUtc = null, int Page = 1, int PageSize = 50);
