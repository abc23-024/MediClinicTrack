using MediClinicTrack.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MediClinicTrack.Tests;

// Starts the real app for tests, with two changes:
// 1. "Testing" environment, so start-up seeding (roles, admin) is skipped.
// 2. A throwaway in-memory database instead of MySQL, so tests run on any
//    machine and in GitHub's build check without a database server.
public class TestAppFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = "MediClinicTrackTests-" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            // Remove the app's MySQL set-up...
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();

            // ...and use an in-memory database instead.
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
        });
    }

    // The login cookie is HTTPS-only, so tests must talk HTTPS too.
    public HttpClient CreateHttpsClient() =>
        CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
}
