using NTNP.Pricing.Contracts.Auth;
using NTNP.Pricing.Desktop.Services;

namespace NTNP.Pricing.Desktop.Tests.TestSupport;

/// <summary>Most view models call the API as soon as they navigate in, so tests need an already-authenticated session.</summary>
public static class AuthenticatedSessionFactory
{
    public static AppSession Create(params string[] roles)
    {
        var session = new AppSession();
        var user = new UserDto(Guid.NewGuid(), "test-user", "test-user@ntnp.local", "Test User", roles.Length == 0 ? new[] { "Admin" } : roles, true, null);
        session.ApplyLogin(new LoginResponse("access-token", DateTimeOffset.UtcNow.AddMinutes(15), "refresh-token", DateTimeOffset.UtcNow.AddDays(7), user), remember: false);
        return session;
    }
}
