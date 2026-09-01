using System.Net;
using System.Net.Http.Json;
using NTNP.Pricing.Contracts.Customers;

namespace NTNP.Pricing.Api.IntegrationTests;

/// <summary>
/// Section 31 — a stale RowVersion must be rejected as a real 409, never silently overwritten. This
/// exercises the actual optimistic-concurrency path (EF Core's rowversion token → DbUpdateConcurrencyException
/// → <c>ExceptionHandlingMiddleware</c>'s 409 translation) over real HTTP, two independent "editors" racing.
/// </summary>
public sealed class ConcurrencyConflictApiTests : IntegrationTestBase
{
    public ConcurrencyConflictApiTests(ApiTestFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Two_Concurrent_Edits_The_Second_With_A_Stale_RowVersion_Gets_A_409_Not_A_Silent_Overwrite()
    {
        var client = await CreateAdminClientAsync();
        var code = $"CUST-{Guid.NewGuid():N}"[..12];

        var createResponse = await client.PostAsJsonAsync("/api/customers", new CreateCustomerRequest(
            code, "Concurrency Test Co.", null, null, null, null, null, null, null, null, null), Json);
        var original = (await createResponse.Content.ReadFromJsonAsync<CustomerDto>(Json))!;

        // "Editor A" loads the record, then successfully saves a change.
        var editorAUpdate = await client.PutAsJsonAsync($"/api/customers/{original.Id}", new UpdateCustomerRequest(
            "Concurrency Test Co. (Editor A)", null, null, null, null, null, null, null, null, null, true, original.RowVersion), Json);
        Assert.Equal(HttpStatusCode.OK, editorAUpdate.StatusCode);

        // "Editor B" loaded the SAME original record (before Editor A's save) and now tries to save
        // using that now-stale RowVersion.
        var editorBUpdate = await client.PutAsJsonAsync($"/api/customers/{original.Id}", new UpdateCustomerRequest(
            "Concurrency Test Co. (Editor B — should be rejected)", null, null, null, null, null, null, null, null, null, true, original.RowVersion), Json);

        Assert.Equal(HttpStatusCode.Conflict, editorBUpdate.StatusCode);

        // Editor A's change must be the one that survives — proving Editor B's stale write never landed.
        var final = await (await client.GetAsync($"/api/customers/{original.Id}")).Content.ReadFromJsonAsync<CustomerDto>(Json);
        Assert.Equal("Concurrency Test Co. (Editor A)", final!.CompanyName);
    }
}
