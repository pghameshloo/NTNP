using NTNP.Pricing.Domain.Common;

namespace NTNP.Pricing.Domain.Entities;

/// <summary>
/// Section 3 — UniSafe / UniGear ZS3.2 / SIVACON 8PT are seeded rows, not a hardcoded enum, so new
/// families can be added by an Admin without a code change.
/// </summary>
public class ProductFamily : SoftDeletableAuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? VoltageRangeDescription { get; set; }
    public string? SwitchgearClass { get; set; } // e.g. "MV withdrawable", "LV withdrawable"
    public string? Notes { get; set; }
}

/// <summary>
/// Section 3 — INCOMING/OUTGOING/BUS COUPLER/... are seeded rows; the system is explicitly required
/// not to hardcode panel types.
/// </summary>
public class PanelType : SoftDeletableAuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
}
