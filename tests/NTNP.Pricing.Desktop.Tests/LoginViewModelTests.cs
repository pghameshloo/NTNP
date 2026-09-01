using System.Net;
using NTNP.Pricing.Contracts.Auth;
using NTNP.Pricing.Contracts.Common;
using NTNP.Pricing.Desktop.Services;
using NTNP.Pricing.Desktop.Services.Api;
using NTNP.Pricing.Desktop.Tests.TestSupport;
using NTNP.Pricing.Desktop.ViewModels;

namespace NTNP.Pricing.Desktop.Tests;

public class LoginViewModelTests
{
    private static readonly UserDto SampleUser = new(Guid.NewGuid(), "admin@ntnp.local", "admin@ntnp.local", "System Administrator", new[] { "Admin" }, true, null);

    private static (LoginViewModel ViewModel, FakeHttpMessageHandler Handler, AppSession Session) Build()
    {
        var handler = new FakeHttpMessageHandler();
        var http = new HttpClient(handler);
        var serverSettings = new FakeServerConnectionSettingsService();
        var session = new AppSession();
        var authApi = new AuthApiClient(http, serverSettings, session);
        var healthApi = new HealthApiClient(http, serverSettings, session);
        var vm = new LoginViewModel(authApi, healthApi, session, serverSettings);
        return (vm, handler, session);
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_AppliesLoginToSessionAndRaisesLoginSucceeded()
    {
        var (vm, handler, session) = Build();
        var response = new LoginResponse("access-token", DateTimeOffset.UtcNow.AddMinutes(15), "refresh-token", DateTimeOffset.UtcNow.AddDays(7), SampleUser);
        handler.WhenJson(HttpMethod.Post, "api/auth/login", response);

        var raised = false;
        vm.LoginSucceeded += () => raised = true;
        vm.UserNameOrEmail = "admin@ntnp.local";

        await vm.LoginAsync("Ntnp!Admin123");

        Assert.True(raised);
        Assert.True(session.IsAuthenticated);
        Assert.Equal("access-token", session.AccessToken);
        Assert.Equal("System Administrator", session.CurrentUser!.DisplayName);
        Assert.Null(vm.ErrorMessage);
    }

    [Fact]
    public async Task LoginAsync_WithWrongCredentials_SetsErrorMessage_AndDoesNotAuthenticate()
    {
        var (vm, handler, session) = Build();
        handler.When(HttpMethod.Post, "api/auth/login", _ => new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = System.Net.Http.Json.JsonContent.Create(new ApiErrorResponse("authentication-failed", "Authentication failed", 401, new[] { "Invalid username or password." })),
        });

        var raised = false;
        vm.LoginSucceeded += () => raised = true;

        await vm.LoginAsync("wrong-password");

        Assert.False(raised);
        Assert.False(session.IsAuthenticated);
        Assert.Contains("Invalid username or password.", vm.ErrorMessage);
    }

    [Fact]
    public async Task TestConnectionAsync_WhenServerReachable_ReportsConnectionOk()
    {
        var (vm, handler, _) = Build();
        handler.WhenJson(HttpMethod.Get, "api/health", new ServerStatusDto("1.0.0", "20260101_InitialCreate", true, DateTimeOffset.UtcNow));

        await vm.TestConnectionCommand.ExecuteAsync(null);

        Assert.True(vm.ConnectionOk);
        Assert.Contains("1.0.0", vm.ConnectionStatus);
    }

    [Fact]
    public async Task TestConnectionAsync_WhenServerUnreachable_ReportsConnectionFailed_WithoutThrowing()
    {
        var (vm, handler, _) = Build();
        handler.When(HttpMethod.Get, "api/health", _ => throw new HttpRequestException("connection refused"));

        await vm.TestConnectionCommand.ExecuteAsync(null);

        Assert.False(vm.ConnectionOk);
        Assert.NotNull(vm.ConnectionStatus);
    }
}
