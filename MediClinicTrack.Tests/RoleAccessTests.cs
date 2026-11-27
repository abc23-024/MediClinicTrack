using System.Collections.Concurrent;
using System.Net;
using System.Text.RegularExpressions;
using MediClinicTrack.Data;
using MediClinicTrack.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace MediClinicTrack.Tests;

// US06-4: every role (and an anonymous visitor) against every route.
// The expected table below is written out by hand from docs/permission-matrix.md, on purpose:
// if someone changes a policy in code, these tests fail until the matrix is updated too.
public class RoleAccessTests : IClassFixture<RoleAccessTests.Fixture>
{
    private const string Anonymous = "Anonymous";
    private static readonly string[] Everyone = AppRoles.All.Prepend(Anonymous).ToArray();

    // Controller/action, URL to request, and who may open it.
    private static readonly (string Action, string Url, string[] Allowed)[] Matrix =
    {
        ("Home/Index", "/", Everyone),
        ("Home/Privacy", "/Home/Privacy", Everyone),
        ("Home/Error", "/Home/Error", Everyone),
        ("Home/Status", "/Home/Status?code=404", Everyone),
        ("PatientPortal/Index", "/PatientPortal", new[] { AppRoles.Patient }),
        ("Reception/Index", "/Reception", new[] { AppRoles.Receptionist }),
        ("Clinical/Index", "/Clinical", new[] { AppRoles.Clinician }),
        ("Reports/Index", "/Reports", new[] { AppRoles.Manager }),
        ("Admin/Index", "/Admin", new[] { AppRoles.Admin }),
    };

    // Identity pages outside Manage/ that need a signed-in user (any role).
    private static readonly string[] SignedInIdentityPages = { "/Identity/Account/AccessDenied" };

    // Built-in two-factor pages that throw on purpose when opened directly, without a
    // two-factor sign-in in progress or 2FA turned on. Not an access problem, and the same on main.
    private static readonly string[] ThrowWhenOpenedOutOfOrder =
    {
        "/Identity/Account/LoginWith2fa",
        "/Identity/Account/LoginWithRecoveryCode",
        "/Identity/Account/Manage/Disable2fa",
    };

    public static TheoryData<string, string> RouteAndRole()
    {
        var data = new TheoryData<string, string>();
        foreach (var (_, url, _) in Matrix)
            foreach (var who in Everyone)
                data.Add(url, who);
        return data;
    }

    private readonly Fixture _fixture;

    public RoleAccessTests(Fixture fixture) => _fixture = fixture;

    [Theory]
    [MemberData(nameof(RouteAndRole))]
    public async Task Route_AllowsOnlyRolesInTheMatrix(string url, string who)
    {
        var allowed = Matrix.Single(m => m.Url == url).Allowed.Contains(who);
        var client = await _fixture.ClientForAsync(who);

        var response = await client.GetAsync(url);

        if (allowed)
            Assert.True(response.IsSuccessStatusCode || (url.Contains("code=404") && response.StatusCode == HttpStatusCode.NotFound),
                $"{who} should be able to open {url} but got {(int)response.StatusCode}.");
        else if (who == Anonymous)
            AssertRedirectsToLogin(response);
        else
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // Stops a new controller action slipping in without a row in the matrix (and a test).
    [Fact]
    public void Matrix_CoversEveryControllerAction()
    {
        var actions = _fixture.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .Select(e => e.Metadata.GetMetadata<ControllerActionDescriptor>())
            .OfType<ControllerActionDescriptor>()
            .Select(d => $"{d.ControllerName}/{d.ActionName}")
            .Distinct()
            .Order();

        Assert.Equal(Matrix.Select(m => m.Action).Order(), actions);
    }

    // Identity pages: Account pages are public, Manage pages need a signed-in user,
    // and no role is refused any of them.
    [Fact]
    public async Task IdentityPages_FollowTheMatrix()
    {
        var pages = _fixture.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .Select(e => e.Metadata.GetMetadata<PageActionDescriptor>())
            .OfType<PageActionDescriptor>()
            .Where(d => d.AreaName == "Identity")
            .Select(d => "/Identity" + d.ViewEnginePath)
            .Distinct()
            .ToList();
        Assert.Contains("/Identity/Account/Login", pages);

        var problems = new List<string>();
        foreach (var page in pages)
        {
            foreach (var who in Everyone)
            {
                var response = await (await _fixture.ClientForAsync(who)).GetAsync(page);
                var status = (int)response.StatusCode;
                // The sign-in challenge adds ?ReturnUrl=; a page's own redirect to Login doesn't.
                var askedToSignIn = response.Headers.Location?.OriginalString
                    .Contains("/Identity/Account/Login?ReturnUrl=") == true;
                var signedInOnly = page.StartsWith("/Identity/Account/Manage") || SignedInIdentityPages.Contains(page);

                if (status == 403)
                    problems.Add($"{who} {page}: 403");
                else if (status >= 500 && !ThrowWhenOpenedOutOfOrder.Contains(page))
                    problems.Add($"{who} {page}: {status}");
                else if (who == Anonymous && signedInOnly && !askedToSignIn)
                    problems.Add($"{who} {page}: expected redirect to login, got {status}");
                else if (who == Anonymous && !signedInOnly && askedToSignIn)
                    problems.Add($"{who} {page}: public page asks for login");
            }
        }

        Assert.True(problems.Count == 0, string.Join("\n", problems));
    }

    [Fact]
    public async Task StaticFiles_ArePublic()
    {
        var response = await (await _fixture.ClientForAsync(Anonymous)).GetAsync("/css/site.css");

        response.EnsureSuccessStatusCode();
    }

    // US06-2: the 403 is a page the user can read, and the refusal is recorded.
    [Fact]
    public async Task RefusedRequest_Shows403Page_AndIsLogged()
    {
        var client = await _fixture.ClientForAsync(AppRoles.Patient);

        var response = await client.GetAsync("/Admin?search=secret");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("Access denied", await response.Content.ReadAsStringAsync());

        var logged = _fixture.Denied.Last(e => e.Path == "/Admin");
        Assert.Equal(_fixture.UserIds[AppRoles.Patient], logged.UserId);
        Assert.Equal(new[] { AppRoles.Patient }, logged.Roles);
        Assert.Equal("GET", logged.Method);
        Assert.Equal(AppRoles.Admin, logged.RequiredRoles);
    }

    // US06-5: a role change applies at the user's next request, without signing in again.
    [Fact]
    public async Task RoleChange_TakesEffectAtNextRequest()
    {
        var (client, userId) = await _fixture.NewSignedInUserAsync(AppRoles.Patient);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/Reception")).StatusCode);

        await _fixture.WithUserAsync(userId, (users, user) => users.AddToRoleAsync(user, AppRoles.Receptionist));
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/Reception")).StatusCode);

        await _fixture.WithUserAsync(userId, (users, user) => users.RemoveFromRoleAsync(user, AppRoles.Receptionist));
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/Reception")).StatusCode);
    }

    private static void AssertRedirectsToLogin(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Identity/Account/Login", response.Headers.Location?.OriginalString);
    }

    // One signed-in browser per role, plus an anonymous one. Users are created in the
    // in-memory database and sign in through the real Login page.
    public class Fixture : TestAppFactory
    {
        public ConcurrentQueue<AccessDeniedEvent> Denied { get; } = new();
        public Dictionary<string, string> UserIds { get; } = new();
        private readonly Dictionary<string, HttpClient> _clients = new();
        private readonly SemaphoreSlim _lock = new(1, 1);
        private bool _rolesCreated;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureTestServices(services =>
                services.AddSingleton<IAccessDeniedLog>(new RecordingLog(Denied)));
        }

        public async Task<HttpClient> ClientForAsync(string who)
        {
            await _lock.WaitAsync();
            try
            {
                if (_clients.TryGetValue(who, out var existing))
                    return existing;

                HttpClient client;
                if (who == Anonymous)
                {
                    client = NewBrowser();
                }
                else
                {
                    (client, var id) = await NewSignedInUserAsync(who);
                    UserIds[who] = id;
                }
                return _clients[who] = client;
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task<(HttpClient Client, string UserId)> NewSignedInUserAsync(string role)
        {
            await EnsureRolesAsync();

            var email = $"{role.ToLowerInvariant()}-{Guid.NewGuid():N}@test.local";
            var password = "Test-" + Guid.NewGuid();
            string userId;
            using (var scope = Services.CreateScope())
            {
                var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
                AssertSucceeded(await users.CreateAsync(user, password));
                AssertSucceeded(await users.AddToRoleAsync(user, role));
                userId = user.Id;
            }

            var client = NewBrowser();
            var loginPage = await client.GetStringAsync("/Identity/Account/Login");
            var token = Regex.Match(loginPage, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;

            var response = await client.PostAsync("/Identity/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Input.Email"] = email,
                ["Input.Password"] = password,
                ["__RequestVerificationToken"] = token,
            }));
            Assert.True(response.StatusCode == HttpStatusCode.Redirect, $"Login as {role} failed: {(int)response.StatusCode}");

            return (client, userId);
        }

        public async Task WithUserAsync(string userId, Func<UserManager<ApplicationUser>, ApplicationUser, Task<IdentityResult>> change)
        {
            using var scope = Services.CreateScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            AssertSucceeded(await change(users, (await users.FindByIdAsync(userId))!));
        }

        private async Task EnsureRolesAsync()
        {
            if (_rolesCreated)
                return;

            using var scope = Services.CreateScope();
            var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            foreach (var role in AppRoles.All)
                if (!await roles.RoleExistsAsync(role))
                    AssertSucceeded(await roles.CreateAsync(new IdentityRole(role)));
            _rolesCreated = true;
        }

        private HttpClient NewBrowser() => CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
        });

        private static void AssertSucceeded(IdentityResult result) =>
            Assert.True(result.Succeeded, string.Join("; ", result.Errors.Select(e => e.Description)));
    }

    private class RecordingLog : IAccessDeniedLog
    {
        private readonly ConcurrentQueue<AccessDeniedEvent> _events;

        public RecordingLog(ConcurrentQueue<AccessDeniedEvent> events) => _events = events;

        public Task RecordAsync(AccessDeniedEvent deniedEvent)
        {
            _events.Enqueue(deniedEvent);
            return Task.CompletedTask;
        }
    }
}
