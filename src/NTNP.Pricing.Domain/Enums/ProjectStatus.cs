namespace NTNP.Pricing.Domain.Enums;

/// <summary>Section 13. Governs the allowed state-machine transitions enforced by ProjectRevision.</summary>
public enum ProjectStatus
{
    Draft = 1,
    UnderEngineeringReview = 2,
    UnderCommercialReview = 3,
    PendingApproval = 4,
    Approved = 5,
    Rejected = 6,
    Locked = 7,
    Superseded = 8,
}
