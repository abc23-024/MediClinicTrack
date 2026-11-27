using Microsoft.AspNetCore.Authorization;

namespace MediClinicTrack.Security;

// US06-1: the permission matrix in code. Each policy names one area of the app and the
// roles allowed into it. docs/permission-matrix.md is the readable version and must be
// kept in step with this file (the tests in RoleAccessTests check both agree with the app).
//
// Put [Authorize(Policy = Policies.X)] on every new controller. Anything without an
// attribute still needs a signed-in user (the fallback policy in Program.cs); only
// pages marked [AllowAnonymous] are public.
public static class Policies
{
    public const string PatientPortal = nameof(PatientPortal);
    public const string Reception = nameof(Reception);
    public const string Clinical = nameof(Clinical);
    public const string Reports = nameof(Reports);
    public const string Administration = nameof(Administration);

    public static readonly IReadOnlyDictionary<string, string[]> Matrix = new Dictionary<string, string[]>
    {
        [PatientPortal] = new[] { AppRoles.Patient },
        [Reception] = new[] { AppRoles.Receptionist },
        [Clinical] = new[] { AppRoles.Clinician },
        [Reports] = new[] { AppRoles.Manager },
        [Administration] = new[] { AppRoles.Admin },
    };

    public static void AddAppPolicies(this AuthorizationOptions options)
    {
        foreach (var (policy, roles) in Matrix)
            options.AddPolicy(policy, p => p.RequireAuthenticatedUser().RequireRole(roles));

        // US06-3: deny by default. Every endpoint needs a signed-in user unless it is marked
        // [AllowAnonymous], so a page someone forgets to protect is not left open.
        options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
    }
}
