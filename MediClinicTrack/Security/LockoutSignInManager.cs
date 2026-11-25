using MediClinicTrack.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace MediClinicTrack.Security;

// US01-4 / US07: the built-in Identity Login page signs in with lockoutOnFailure: false,
// so failed attempts are never counted. This forces lockout on for every password sign-in,
// whichever page calls it (including the login pages once they are copied into the project).
public class LockoutSignInManager : SignInManager<ApplicationUser>
{
    public LockoutSignInManager(
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

    public override Task<SignInResult> PasswordSignInAsync(
        ApplicationUser user, string password, bool isPersistent, bool lockoutOnFailure) =>
        base.PasswordSignInAsync(user, password, isPersistent, lockoutOnFailure: true);
}
