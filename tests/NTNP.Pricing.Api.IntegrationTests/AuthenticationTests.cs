using System.Net;
using System.Net.Http.Json;
using NTNP.Pricing.Contracts.Auth;
using NTNP.Pricing.Domain.Enums;
using NTNP.Pricing.Infrastructure.Seed;

namespace NTNP.Pricing.Api.IntegrationTests;

/// <summary>
/// Section 6 — "Hiding buttons is not authorization": every endpoint enforces its own server-side
/// policy. These tests hit the real JWT bearer pipeline + policy handlers, not a mock.
/// </summary>
public sealed class AuthenticationTests : IntegrationTestBase
{
    public AuthenticationTests(ApiTestFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Login_With_Seeded_Admin_Credentials_Returns_A_Valid_Token_And_Admin_Role()
    {
        var client = Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(IdentitySeeder.DevAdminEmail, IdentitySeeder.DevAdminPassword), Json);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var login = await response.Content.ReadFromJsonAsync<LoginResponse>(Json);
        Assert.NotNull(login);
        Assert.False(string.IsNullOrWhiteSpace(login!.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(login.RefreshToken));
        Assert.Contains(Roles.Admin, login.User.Roles);
        Assert.True(login.AccessTokenExpiresAtUtc > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Login_With_Wrong_Password_Returns_Unauthorized_Not_500()
    {
        var client = Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(IdentitySeeder.DevAdminEmail, "TotallyWrongPassword!1"), Json);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_With_Valid_RefreshToken_Rotates_It_And_Issues_A_New_Access_Token()
    {
        var client = Factory.CreateClient();
        var first = (await (await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(IdentitySeeder.DevAdminEmail, IdentitySeeder.DevAdminPassword), Json))
            .Content.ReadFromJsonAsync<LoginResponse>(Json))!;

        var refreshResponse = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest(first.RefreshToken), Json);

        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        var refreshed = await refreshResponse.Content.ReadFromJsonAsync<LoginResponse>(Json);
        Assert.NotNull(refreshed);
        Assert.NotEqual(first.AccessToken, refreshed!.AccessToken);
        Assert.NotEqual(first.RefreshToken, refreshed.RefreshToken); // rotation (Section 6/7)

        // The old refresh token must now be rejected — reuse of a rotated-out token is an attack signal.
        var reuseResponse = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest(first.RefreshToken), Json);
        Assert.Equal(HttpStatusCode.Unauthorized, reuseResponse.StatusCode);
    }

    [Fact]
    public async Task Anonymous_Request_To_A_Protected_Endpoint_Returns_401()
    {
        var client = Factory.CreateClient();

        var response = await client.GetAsync("/api/customers");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Health_Endpoint_Is_Reachable_Anonymously()
    {
        var client = Factory.CreateClient();

        var response = await client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Viewer_Role_Cannot_Create_A_Customer_ManageCustomers_Is_Admin_Or_Commercial_Only()
    {
        var adminClient = await CreateAdminClientAsync();

        // Create a Viewer-role user via the real Users endpoint (Section 6/37), then prove RBAC
        // actually blocks the create-customer action for that role server-side.
        var email = $"viewer-{Guid.NewGuid():N}@ntnp.local";
        var createUser = await adminClient.PostAsJsonAsync("/api/users", new CreateUserRequest(email, email, "Viewer Test User", "Ntnp!Viewer123", new[] { Roles.Viewer }), Json);
        Assert.Equal(HttpStatusCode.Created, createUser.StatusCode);

        var viewerClient = await CreateAuthenticatedClientAsync(email, "Ntnp!Viewer123");

        var response = await viewerClient.PostAsync("/api/customers", AsJson(new
        {
            CustomerCode = "CUST-RBAC-TEST",
            CompanyName = "RBAC Test Co.",
            ContactPerson = (string?)null,
            Email = (string?)null,
            Phone = (string?)null,
            Address = (string?)null,
        }));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
