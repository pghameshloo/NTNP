namespace NTNP.Pricing.Domain.Common;

/// <summary>
/// Base type for every persisted aggregate/entity. <see cref="RowVersion"/> backs SQL Server
/// optimistic concurrency (Section 31) — EF Core maps it to a `rowversion` column.
/// </summary>
public abstract class Entity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// SQL Server ROWVERSION token, used for optimistic-concurrency conflict detection. Left as the
    /// CLR default (null) rather than initialized to an empty array — EF Core treats a non-default
    /// value as "already set by the app" and would skip database-side generation on insert.
    /// </summary>
    public byte[] RowVersion { get; set; } = null!;
}

/// <summary>
/// Adds the standard created/updated audit stamp columns required across all business entities
/// (Section 30). User identity is stored both as an id (FK into Identity, owned by Infrastructure)
/// and a denormalized display-name snapshot so audit trails remain readable even if a user account
/// is later renamed or deactivated.
/// </summary>
public abstract class AuditableEntity : Entity
{
    public Guid CreatedByUserId { get; set; }
    public string CreatedByUserName { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public Guid? UpdatedByUserId { get; set; }
    public string? UpdatedByUserName { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
}

/// <summary>
/// Adds soft-deletion / active-inactive semantics used by master-data entities that must never be
/// hard-deleted once referenced by history (equipment, currencies, customers, templates, users).
/// </summary>
public abstract class SoftDeletableAuditableEntity : AuditableEntity
{
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAtUtc { get; set; }
    public Guid? DeletedByUserId { get; set; }
}
