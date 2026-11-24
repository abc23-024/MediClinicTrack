using MediClinicTrack.Data;
using Microsoft.AspNetCore.Identity;

namespace MediClinicTrack.Security;

// US07: store passwords with bcrypt instead of Identity's default PBKDF2.
public class BcryptPasswordHasher : IPasswordHasher<ApplicationUser>
{
    private const int WorkFactor = 12;

    public string HashPassword(ApplicationUser user, string password) =>
        BCrypt.Net.BCrypt.HashPassword(password, workFactor: WorkFactor);

    public PasswordVerificationResult VerifyHashedPassword(
        ApplicationUser user, string hashedPassword, string providedPassword)
    {
        if (string.IsNullOrEmpty(hashedPassword))
            return PasswordVerificationResult.Failed;

        try
        {
            return BCrypt.Net.BCrypt.Verify(providedPassword, hashedPassword)
                ? PasswordVerificationResult.Success
                : PasswordVerificationResult.Failed;
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // Hash isn't bcrypt (e.g. an old PBKDF2 hash): treat as a failed login rather than crashing.
            return PasswordVerificationResult.Failed;
        }
    }
}
