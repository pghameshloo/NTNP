using System.Net;
using System.Net.Http.Json;
using NTNP.Pricing.Contracts.Common;
using NTNP.Pricing.Contracts.Customers;

namespace NTNP.Pricing.Api.IntegrationTests;

/// <summary>Section 7 — full customer master-data CRUD round-trip through the real HTTP pipeline.</summary>
public sealed class CustomersApiTests : IntegrationTestBase
{
    public CustomersApiTests(ApiTestFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Create_Then_Get_Then_Update_Then_List_RoundTrips_Through_The_Real_Api()
    {
        var client = await CreateAdminClientAsync();
        var code = $"CUST-{Guid.NewGuid():N}"[..12];

        var createResponse = await client.PostAsJsonAsync("/api/customers", new CreateCustomerRequest(
            code, "Acme Switchgear Co.", "Industrial", null, null, "Jane Doe", "Procurement Manager",
            "+98-21-0000000", "jane@example.com", "Tehran, Iran", "Created by integration test"), Json);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<CustomerDto>(Json);
        Assert.NotNull(created);
        Assert.Equal(code, created!.CustomerCode);
        Assert.True(created.IsActive);

        var getResponse = await client.GetAsync($"/api/customers/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var fetched = await getResponse.Content.ReadFromJsonAsync<CustomerDto>(Json);
        Assert.Equal(created.CompanyName, fetched!.CompanyName);

        var updateResponse = await client.PutAsJsonAsync($"/api/customers/{created.Id}", new UpdateCustomerRequest(
            "Acme Switchgear Company Ltd.", fetched.Industry, fetched.RegistrationNumber, fetched.TaxId,
            fetched.ContactPerson, fetched.ContactPosition, fetched.Phone, fetched.Email, fetched.Address,
            "Updated by integration test", true, fetched.RowVersion), Json);

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<CustomerDto>(Json);
        Assert.Equal("Acme Switchgear Company Ltd.", updated!.CompanyName);

        var listResponse = await client.GetAsync($"/api/customers?search={Uri.EscapeDataString(code)}");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var page = await listResponse.Content.ReadFromJsonAsync<PagedResult<CustomerDto>>(Json);
        Assert.Contains(page!.Items, c => c.Id == created.Id);
    }

    [Fact]
    public async Task Create_With_Missing_Required_Field_Returns_400_With_Validation_Errors()
    {
        var client = await CreateAdminClientAsync();

        var response = await client.PostAsJsonAsync("/api/customers", new CreateCustomerRequest(
            "", "", null, null, null, null, null, null, "not-an-email", null, null), Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(Json);
        Assert.NotEmpty(body!.Errors);
    }

    [Fact]
    public async Task Get_Nonexistent_Customer_Returns_404()
    {
        var client = await CreateAdminClientAsync();

        var response = await client.GetAsync($"/api/customers/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
