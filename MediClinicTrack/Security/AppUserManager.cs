using MediClinicTrack.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace MediClinicTrack.Security;

// Clears ApplicationUser.MustChangePassword once the user has set a new password,
// whether through Manage > Change password or a password-reset link.
public class AppUserManager : UserManager<ApplicationUser>
{
    public AppUserManager(
        IUserStore<ApplicationUser> store,
        IOptions<IdentityOptions> optionsAccessor,
        IPasswordHasher<ApplicationUser> passwordHasher,
        IEnumerable<IUserValidator<ApplicationUser>> userValidators,
        IEnumerable<IPasswordValidator<ApplicationUser>> passwordValidators,
        ILookupNormalizer keyNormalizer,
        IdentityErrorDescriber errors,
        IServiceProvider services,
        ILogger<UserManager<ApplicationUser>> logger)
        : base(store, optionsAccessor, passwordHasher, userValidators, passwordValidators,
               keyNormalizer, errors, services, logger)
    {
    }

    public override async Task<IdentityResult> ChangePasswordAsync(
        ApplicationUser user, string currentPassword, string newPassword) =>
        await ClearMustChangePasswordIfSucceeded(user, await base.ChangePasswordAsync(user, currentPassword, newPassword));

    public override async Task<IdentityResult> ResetPasswordAsync(
        ApplicationUser user, string token, string newPassword) =>
        await ClearMustChangePasswordIfSucceeded(user, await base.ResetPasswordAsync(user, token, newPassword));

    private async Task<IdentityResult> ClearMustChangePasswordIfSucceeded(ApplicationUser user, IdentityResult result)
    {
        if (!result.Succeeded || !user.MustChangePassword)
            return result;

        user.MustChangePassword = false;
        return await UpdateAsync(user);
    }
}
