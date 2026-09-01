namespace NTNP.Pricing.Domain.Enums;

/// <summary>Section 30 — the closed set of audited action kinds.</summary>
public enum AuditAction
{
    Created = 1,
    Updated = 2,
    Deleted = 3,
    Activated = 4,
    Deactivated = 5,
    PriceChanged = 6,
    ExchangeRateChanged = 7,
    BomChanged = 8,
    BodyEsChanged = 9,
    TemplateApproved = 10,
    ProjectLineChanged = 11,
    PricingOverride = 12,
    RevisionCreated = 13,
    Approved = 14,
    Rejected = 15,
    Locked = 16,
    Unlocked = 17,
    Imported = 18,
    Exported = 19,
    ReportIssued = 20,
    UserRoleChanged = 21,
    LoginSucceeded = 22,
    LoginFailed = 23,
}

/// <summary>Section 32.</summary>
public enum FileCategory
{
    ImportedExcel = 1,
    ProjectAttachment = 2,
    GeneratedQuotation = 3,
    InternalReport = 4,
    BomMtoExport = 5,
    ApprovalDocument = 6,
}
