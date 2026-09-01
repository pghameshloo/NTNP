using Microsoft.EntityFrameworkCore;
using NTNP.Pricing.Application.Common;
using NTNP.Pricing.Application.Exceptions;
using NTNP.Pricing.Contracts.Common;
using NTNP.Pricing.Contracts.Customers;
using NTNP.Pricing.Domain.Entities;
using NTNP.Pricing.Domain.Enums;

namespace NTNP.Pricing.Application.Customers;

/// <summary>Section 7 — Customer master data: search/filter/sort, duplicate detection, audit trail.</summary>
public sealed class CustomerService : ICustomerService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditService _audit;

    public CustomerService(IApplicationDbContext db, ICurrentUserService currentUser, IDateTimeProvider clock, IAuditService audit)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _audit = audit;
    }

    public async Task<PagedResult<CustomerDto>> SearchAsync(PagedQuery query, bool includeInactive, CancellationToken ct = default)
    {
        var q = _db.Customers.AsNoTracking().AsQueryable();
        if (!includeInactive) q = q.Where(c => c.IsActive);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            q = q.Where(c =>
                c.CompanyName.Contains(term) || c.CustomerCode.Contains(term) ||
                (c.ContactPerson != null && c.ContactPerson.Contains(term)) ||
                (c.Email != null && c.Email.Contains(term)));
        }

        q = query.SortBy switch
        {
            "code" => query.SortDescending ? q.OrderByDescending(c => c.CustomerCode) : q.OrderBy(c => c.CustomerCode),
            "createdAt" => query.SortDescending ? q.OrderByDescending(c => c.CreatedAtUtc) : q.OrderBy(c => c.CreatedAtUtc),
            _ => query.SortDescending ? q.OrderByDescending(c => c.CompanyName) : q.OrderBy(c => c.CompanyName),
        };

        var total = await q.CountAsync(ct);
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return new PagedResult<CustomerDto>(items.Select(ToDto).ToList(), total, page, pageSize);
    }

    public async Task<CustomerDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var customer = await _db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException(nameof(Customer), id);
        return ToDto(customer);
    }

    public async Task<IReadOnlyList<CustomerDuplicateCandidate>> FindDuplicatesAsync(
        string companyName, string? email, string? phone, CancellationToken ct = default)
    {
        var normalizedName = companyName.Trim().ToLowerInvariant();
        var candidates = await _db.Customers.AsNoTracking()
            .Where(c => c.IsActive)
            .Where(c =>
                c.CompanyName.ToLower() == normalizedName ||
                (email != null && c.Email != null && c.Email.ToLower() == email.ToLower()) ||
                (phone != null && c.Phone != null && c.Phone == phone))
            .ToListAsync(ct);

        return candidates.Select(c => new CustomerDuplicateCandidate(
            c.Id, c.CustomerCode, c.CompanyName,
            c.CompanyName.Equals(companyName, StringComparison.OrdinalIgnoreCase) ? "Company name matches"
                : c.Email != null && email != null && c.Email.Equals(email, StringComparison.OrdinalIgnoreCase) ? "Email matches"
                : "Phone matches")).ToList();
    }

    public async Task<CustomerDto> CreateAsync(CreateCustomerRequest request, CancellationToken ct = default)
    {
        if (await _db.Customers.AnyAsync(c => c.CustomerCode == request.CustomerCode, ct))
            throw new Domain.Exceptions.DomainValidationException($"Customer code '{request.CustomerCode}' already exists.");

        var customer = new Customer
        {
            CustomerCode = request.CustomerCode,
            CompanyName = request.CompanyName,
            Industry = request.Industry,
            RegistrationNumber = request.RegistrationNumber,
            TaxId = request.TaxId,
            ContactPerson = request.ContactPerson,
            ContactPosition = request.ContactPosition,
            Phone = request.Phone,
            Email = request.Email,
            Address = request.Address,
            Notes = request.Notes,
            CreatedByUserId = _currentUser.UserId,
            CreatedByUserName = _currentUser.UserName,
            CreatedAtUtc = _clock.UtcNow,
        };

        _db.Customers.Add(customer);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(AuditAction.Created, nameof(Customer), customer.Id.ToString(), newValue: customer, cancellationToken: ct);

        return ToDto(customer);
    }

    public async Task<CustomerDto> UpdateAsync(Guid id, UpdateCustomerRequest request, CancellationToken ct = default)
    {
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException(nameof(Customer), id);

        _db.Entry(customer).Property(c => c.RowVersion).OriginalValue = request.RowVersion;

        var oldSnapshot = new { customer.CompanyName, customer.IsActive };

        customer.CompanyName = request.CompanyName;
        customer.Industry = request.Industry;
        customer.RegistrationNumber = request.RegistrationNumber;
        customer.TaxId = request.TaxId;
        customer.ContactPerson = request.ContactPerson;
        customer.ContactPosition = request.ContactPosition;
        customer.Phone = request.Phone;
        customer.Email = request.Email;
        customer.Address = request.Address;
        customer.Notes = request.Notes;
        customer.IsActive = request.IsActive;
        customer.UpdatedByUserId = _currentUser.UserId;
        customer.UpdatedByUserName = _currentUser.UserName;
        customer.UpdatedAtUtc = _clock.UtcNow;

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(AuditAction.Updated, nameof(Customer), customer.Id.ToString(), oldSnapshot, new { customer.CompanyName, customer.IsActive }, cancellationToken: ct);

        return ToDto(customer);
    }

    public async Task DeactivateAsync(Guid id, byte[] rowVersion, CancellationToken ct = default)
    {
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException(nameof(Customer), id);

        _db.Entry(customer).Property(c => c.RowVersion).OriginalValue = rowVersion;
        customer.IsActive = false;
        customer.UpdatedByUserId = _currentUser.UserId;
        customer.UpdatedByUserName = _currentUser.UserName;
        customer.UpdatedAtUtc = _clock.UtcNow;

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(AuditAction.Deactivated, nameof(Customer), customer.Id.ToString(), cancellationToken: ct);
    }

    private static CustomerDto ToDto(Customer c) => new(
        c.Id, c.CustomerCode, c.CompanyName, c.Industry, c.RegistrationNumber, c.TaxId,
        c.ContactPerson, c.ContactPosition, c.Phone, c.Email, c.Address, c.Notes, c.IsActive,
        c.CreatedByUserName, c.CreatedAtUtc, c.UpdatedByUserName, c.UpdatedAtUtc, c.RowVersion);
}
