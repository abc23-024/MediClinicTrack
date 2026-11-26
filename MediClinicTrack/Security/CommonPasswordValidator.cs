using MediClinicTrack.Data;
using Microsoft.AspNetCore.Identity;

namespace MediClinicTrack.Security;

// US07-2: reject commonly used passwords. The list (common-passwords.txt, embedded in the
// assembly) holds leaked passwords of 10+ characters; shorter ones already fail the length rule.
public class CommonPasswordValidator : IPasswordValidator<ApplicationUser>
{
    private const string ResourceName = "MediClinicTrack.Security.common-passwords.txt";

    private static readonly Lazy<HashSet<string>> CommonPasswords = new(LoadList);

    public static int Count => CommonPasswords.Value.Count;

    public Task<IdentityResult> ValidateAsync(
        UserManager<ApplicationUser> manager, ApplicationUser user, string? password)
    {
        if (password is not null && CommonPasswords.Value.Contains(password.Trim().ToLowerInvariant()))
        {
            return Task.FromResult(IdentityResult.Failed(new IdentityError
            {
                Code = "CommonPassword",
                Description = "This password is too common and easy to guess. Choose a different one."
            }));
        }

        return Task.FromResult(IdentityResult.Success);
    }

    private static HashSet<string> LoadList()
    {
        using var stream = typeof(CommonPasswordValidator).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{ResourceName}' not found.");
        using var reader = new StreamReader(stream);

        var set = new HashSet<string>(StringComparer.Ordinal);
        while (reader.ReadLine() is { } line)
        {
            if (line.Length > 0 && !line.StartsWith('#'))
                set.Add(line);
        }
        return set;
    }
}
