using NTNP.Pricing.Application.Customers;
using NTNP.Pricing.Application.Tests.TestDoubles;
using NTNP.Pricing.Contracts.Customers;
using NTNP.Pricing.Domain.Exceptions;
using NTNP.Pricing.Infrastructure.Persistence;

namespace NTNP.Pricing.Application.Tests;

public class CustomerServiceTests : IDisposable
{
    private readonly ApplicationDbContext _db = TestDbContextFactory.Create();
    private readonly CustomerService _service;

    public CustomerServiceTests()
    {
        _service = new CustomerService(_db, new FakeCurrentUserService(), new FakeDateTimeProvider(), new NoOpAuditService());
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task CreateAsync_Rejects_Duplicate_CustomerCode()
    {
        await _service.CreateAsync(new CreateCustomerRequest("C-1", "Acme Co.", null, null, null, null, null, null, null, null, null));

        await Assert.ThrowsAsync<DomainValidationException>(() =>
            _service.CreateAsync(new CreateCustomerRequest("C-1", "Different Co.", null, null, null, null, null, null, null, null, null)));
    }

    [Fact]
    public async Task FindDuplicatesAsync_Detects_Matching_CompanyName()
    {
        await _service.CreateAsync(new CreateCustomerRequest("C-2", "Acme Industries", null, null, null, null, null, null, "a@acme.example", null, null));

        var duplicates = await _service.FindDuplicatesAsync("Acme Industries", null, null);

        Assert.Single(duplicates);
    }

    [Fact]
    public async Task SearchAsync_Filters_By_Search_Term()
    {
        await _service.CreateAsync(new CreateCustomerRequest("C-3", "Alpha Corp", null, null, null, null, null, null, null, null, null));
        await _service.CreateAsync(new CreateCustomerRequest("C-4", "Beta Corp", null, null, null, null, null, null, null, null, null));

        var result = await _service.SearchAsync(new Contracts.Common.PagedQuery("Alpha"), includeInactive: false);

        var item = Assert.Single(result.Items);
        Assert.Equal("Alpha Corp", item.CompanyName);
    }
}
