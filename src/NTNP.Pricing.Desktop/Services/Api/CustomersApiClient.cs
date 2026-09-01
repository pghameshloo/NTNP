using NTNP.Pricing.Contracts.Common;
using NTNP.Pricing.Contracts.Customers;

namespace NTNP.Pricing.Desktop.Services.Api;

public sealed class CustomersApiClient : ApiClientBase
{
    public CustomersApiClient(HttpClient http, IServerConnectionSettingsService serverSettings, AppSession session) : base(http, serverSettings, session)
    {
    }

    public Task<PagedResult<CustomerDto>> SearchAsync(string? search, int page, int pageSize, bool includeInactive, CancellationToken ct = default) =>
        GetAsync<PagedResult<CustomerDto>>($"api/customers?search={Uri.EscapeDataString(search ?? "")}&page={page}&pageSize={pageSize}&includeInactive={includeInactive}", ct);

    public Task<CustomerDto> GetAsync(Guid id, CancellationToken ct = default) => GetAsync<CustomerDto>($"api/customers/{id}", ct);

    public Task<IReadOnlyList<CustomerDuplicateCandidate>> FindDuplicatesAsync(string companyName, string? email, string? phone, CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<CustomerDuplicateCandidate>>($"api/customers/duplicates?companyName={Uri.EscapeDataString(companyName)}&email={Uri.EscapeDataString(email ?? "")}&phone={Uri.EscapeDataString(phone ?? "")}", ct);

    public Task<CustomerDto> CreateAsync(CreateCustomerRequest request, CancellationToken ct = default) => PostAsync<CustomerDto>("api/customers", request, ct);
    public Task<CustomerDto> UpdateAsync(Guid id, UpdateCustomerRequest request, CancellationToken ct = default) => PutAsync<CustomerDto>($"api/customers/{id}", request, ct);
}
