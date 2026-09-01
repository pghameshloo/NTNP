using NTNP.Pricing.Domain.Common;

namespace NTNP.Pricing.Domain.Entities;

/// <summary>Section 7 — Customer master data.</summary>
public class Customer : SoftDeletableAuditableEntity
{
    public string CustomerCode { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string? Industry { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? TaxId { get; set; }
    public string? ContactPerson { get; set; }
    public string? ContactPosition { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }

    public ICollection<Project> Projects { get; set; } = new List<Project>();
}
