using System.Security.Claims;
using MediClinicTrack.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace MediClinicTrack.Security;

// Adds a claim when the user must change their password, so the request pipeline can
// send them to the Change password page without a database lookup of its own.
// The principal is rebuilt on every request (ValidationInterval is zero), so the claim
// disappears as soon as the password has been changed.
public class AppClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>
{
    public const string MustChangePasswordClaim = "mct:must_change_password";

    public AppClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IOptions<IdentityOptions> options)
        : base(userManager, roleManager, options)
    {
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        if (user.MustChangePassword)
            identity.AddClaim(new Claim(MustChangePasswordClaim, "true"));
        return identity;
    }
}
