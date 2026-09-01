using NTNP.Pricing.Contracts.Common;
using NTNP.Pricing.Contracts.Customers;

namespace NTNP.Pricing.Application.Customers;

public interface ICustomerService
{
    Task<PagedResult<CustomerDto>> SearchAsync(PagedQuery query, bool includeInactive, CancellationToken ct = default);
    Task<CustomerDto> GetAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<CustomerDuplicateCandidate>> FindDuplicatesAsync(string companyName, string? email, string? phone, CancellationToken ct = default);
    Task<CustomerDto> CreateAsync(CreateCustomerRequest request, CancellationToken ct = default);
    Task<CustomerDto> UpdateAsync(Guid id, UpdateCustomerRequest request, CancellationToken ct = default);
    Task DeactivateAsync(Guid id, byte[] rowVersion, CancellationToken ct = default);
}
