using MediClinicTrack.Data;
using MediClinicTrack.Security;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MediClinicTrack.Tests;

// US07: checks the security settings in Program.cs match the acceptance criteria.
// If someone changes a setting by accident, these tests fail.
public class IdentitySettingsTests : IClassFixture<TestAppFactory>
{
    private readonly TestAppFactory _factory;

    public IdentitySettingsTests(TestAppFactory factory) => _factory = factory;

    private IdentityOptions Identity =>
        _factory.Services.GetRequiredService<IOptions<IdentityOptions>>().Value;

    [Fact]
    public void Password_MustBeAtLeast10Characters()
    {
        Assert.Equal(10, Identity.Password.RequiredLength);
    }

    [Fact]
    public void Account_LocksAfter5FailedLogins()
    {
        Assert.Equal(5, Identity.Lockout.MaxFailedAccessAttempts);
        Assert.True(Identity.Lockout.AllowedForNewUsers);
    }

    [Fact]
    public void Lockout_Lasts15Minutes()
    {
        Assert.Equal(TimeSpan.FromMinutes(15), Identity.Lockout.DefaultLockoutTimeSpan);
    }

    [Fact]
    public void Session_ExpiresAfter30Minutes_AndCookieIsHttpsOnly()
    {
        var cookie = _factory.Services
            .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(IdentityConstants.ApplicationScheme);

        Assert.Equal(TimeSpan.FromMinutes(30), cookie.ExpireTimeSpan);
        Assert.True(cookie.SlidingExpiration);
        Assert.True(cookie.Cookie.HttpOnly);
        Assert.Equal(CookieSecurePolicy.Always, cookie.Cookie.SecurePolicy);
    }

    [Fact]
    public void Passwords_AreStoredWithBcrypt()
    {
        using var scope = _factory.Services.CreateScope();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<ApplicationUser>>();

        Assert.IsType<BcryptPasswordHasher>(hasher);
        Assert.StartsWith("$2", hasher.HashPassword(new ApplicationUser(), "Example12345"));
    }
}
