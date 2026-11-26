using Microsoft.AspNetCore.Identity;

namespace MediClinicTrack.Data;

// Creates the roles and the first Admin account at start-up (US06 / US08).
public static class SeedData
{
    public static readonly string[] Roles = { "Patient", "Receptionist", "Clinician", "Manager", "Admin" };

    public static async Task InitialiseAsync(IServiceProvider services, IConfiguration configuration)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(SeedData));

        foreach (var role in Roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(role));
                ThrowIfFailed(result, $"create role '{role}'");
            }
        }

        if ((await userManager.GetUsersInRoleAsync("Admin")).Count > 0)
            return;

        // Credentials come from user secrets only, never from code.
        var email = configuration["SeedAdmin:Email"];
        var password = configuration["SeedAdmin:Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning(
                "No Admin user exists and SeedAdmin:Email / SeedAdmin:Password are not set in user secrets. Skipping admin seeding.");
            return;
        }

        var admin = await userManager.FindByEmailAsync(email);
        if (admin is null)
        {
            // The seeded password lives in user secrets, so the admin must replace it at first login.
            admin = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true, MustChangePassword = true };
            ThrowIfFailed(await userManager.CreateAsync(admin, password), "create the seed Admin user");
        }

        ThrowIfFailed(await userManager.AddToRoleAsync(admin, "Admin"), "add the seed user to the Admin role");
        logger.LogInformation("Seeded Admin user {Email}.", email);
    }

    private static void ThrowIfFailed(IdentityResult result, string action)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException(
                $"Seeding failed to {action}: {string.Join("; ", result.Errors.Select(e => e.Description))}");
    }
}
