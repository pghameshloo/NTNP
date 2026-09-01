namespace NTNP.Pricing.Contracts.Customers;

public sealed record CustomerDto(
    Guid Id,
    string CustomerCode,
    string CompanyName,
    string? Industry,
    string? RegistrationNumber,
    string? TaxId,
    string? ContactPerson,
    string? ContactPosition,
    string? Phone,
    string? Email,
    string? Address,
    string? Notes,
    bool IsActive,
    string CreatedByUserName,
    DateTimeOffset CreatedAtUtc,
    string? UpdatedByUserName,
    DateTimeOffset? UpdatedAtUtc,
    byte[] RowVersion);

public sealed record CreateCustomerRequest(
    string CustomerCode,
    string CompanyName,
    string? Industry,
    string? RegistrationNumber,
    string? TaxId,
    string? ContactPerson,
    string? ContactPosition,
    string? Phone,
    string? Email,
    string? Address,
    string? Notes);

public sealed record UpdateCustomerRequest(
    string CompanyName,
    string? Industry,
    string? RegistrationNumber,
    string? TaxId,
    string? ContactPerson,
    string? ContactPosition,
    string? Phone,
    string? Email,
    string? Address,
    string? Notes,
    bool IsActive,
    byte[] RowVersion);

/// <summary>Section 7 — duplicate detection result surfaced before create/update is confirmed.</summary>
public sealed record CustomerDuplicateCandidate(Guid Id, string CustomerCode, string CompanyName, string MatchReason);
