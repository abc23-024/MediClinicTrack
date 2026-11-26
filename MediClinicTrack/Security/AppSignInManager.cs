using System.Security.Claims;
using MediClinicTrack.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace MediClinicTrack.Security;

// Sign-in rules that must hold whichever page signs a user in or out
// (built-in Identity pages or the copies under Areas/Identity).
public class AppSignInManager : SignInManager<ApplicationUser>
{
    public AppSignInManager(
        UserManager<ApplicationUser> userManager,
        IHttpContextAccessor contextAccessor,
        IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory,
        IOptions<IdentityOptions> optionsAccessor,
        ILogger<SignInManager<ApplicationUser>> logger,
        IAuthenticationSchemeProvider schemes,
        IUserConfirmation<ApplicationUser> confirmation)
        : base(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
    {
    }

    // US01-4: always count failed passwords towards lockout. The Login page passes
    // lockoutOnFailure: false by default, which would make the lockout rule do nothing.
    public override Task<SignInResult> PasswordSignInAsync(
        ApplicationUser user, string password, bool isPersistent, bool lockoutOnFailure) =>
        base.PasswordSignInAsync(user, password, isPersistent, lockoutOnFailure: true);

    // US08-2: a deactivated account cannot sign in. The Login page then shows its generic
    // "Invalid login attempt." message, so it doesn't reveal that the account exists (US01-3).
    public override async Task<bool> CanSignInAsync(ApplicationUser user) =>
        user.IsActive && await base.CanSignInAsync(user);

    // US08-2 / US06-5: runs on every request (ValidationInterval is zero in Program.cs).
    // Returning null signs the user out, so a deactivated user is logged out at their next
    // request, and an old cookie stops working once the security stamp has changed.
    public override async Task<ApplicationUser?> ValidateSecurityStampAsync(ClaimsPrincipal? principal)
    {
        var user = await base.ValidateSecurityStampAsync(principal);
        return user is { IsActive: true } ? user : null;
    }

    // US07-3: invalidate the session at logout. Changing the security stamp makes every cookie
    // issued before now fail validation, so a copied cookie can't be replayed after logout.
    // Side effect: it also signs the user out on their other devices.
    public override async Task SignOutAsync()
    {
        if (Context.User.Identity?.IsAuthenticated == true)
        {
            var user = await UserManager.GetUserAsync(Context.User);
            if (user is not null)
                await UserManager.UpdateSecurityStampAsync(user);
        }

        await base.SignOutAsync();
    }
}
