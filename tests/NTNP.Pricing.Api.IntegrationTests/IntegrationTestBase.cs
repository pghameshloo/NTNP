using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using NTNP.Pricing.Contracts.Auth;
using NTNP.Pricing.Infrastructure.Seed;

namespace NTNP.Pricing.Api.IntegrationTests;

/// <summary>Shared HTTP/JSON/auth plumbing for every integration test class. One factory (and one InMemory DB) per test class.</summary>
public abstract class IntegrationTestBase : IClassFixture<ApiTestFactory>
{
    protected static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    protected readonly ApiTestFactory Factory;

    protected IntegrationTestBase(ApiTestFactory factory) => Factory = factory;

    /// <summary>Section 6 — logs in as the seeded development Admin user (Section 37) and returns an authorized client.</summary>
    protected async Task<HttpClient> CreateAdminClientAsync() =>
        await CreateAuthenticatedClientAsync(IdentitySeeder.DevAdminEmail, IdentitySeeder.DevAdminPassword);

    protected async Task<HttpClient> CreateAuthenticatedClientAsync(string userNameOrEmail, string password)
    {
        var anon = Factory.CreateClient();
        var response = await anon.PostAsJsonAsync("/api/auth/login", new LoginRequest(userNameOrEmail, password), Json);
        response.EnsureSuccessStatusCode();
        var login = (await response.Content.ReadFromJsonAsync<LoginResponse>(Json))!;

        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        return client;
    }

    protected static StringContent AsJson(object value) =>
        new(JsonSerializer.Serialize(value, Json), Encoding.UTF8, "application/json");
}
