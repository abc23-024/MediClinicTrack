using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace MediClinicTrack.Security;

// Setup task: the seeded admin (and anyone else flagged MustChangePassword) can't use the
// app until they set their own password. Every page except Change password and Logout
// redirects to Change password. Static files (anything with a file extension) still load.
public class MustChangePasswordMiddleware
{
    public const string ChangePasswordPath = "/Identity/Account/Manage/ChangePassword";
    private const string LogoutPath = "/Identity/Account/Logout";

    private readonly RequestDelegate _next;

    public MustChangePasswordMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, ITempDataDictionaryFactory tempDataFactory)
    {
        var path = context.Request.Path;

        if (context.User.HasClaim(AppClaimsPrincipalFactory.MustChangePasswordClaim, "true")
            && !path.StartsWithSegments(ChangePasswordPath, StringComparison.OrdinalIgnoreCase)
            && !path.StartsWithSegments(LogoutPath, StringComparison.OrdinalIgnoreCase)
            && !Path.HasExtension(path.Value))
        {
            // Shown at the top of the Change password page (its StatusMessage).
            var tempData = tempDataFactory.GetTempData(context);
            tempData["StatusMessage"] = "You must change your password before you can continue.";
            tempData.Save();

            context.Response.Redirect(ChangePasswordPath);
            return;
        }

        await _next(context);
    }
}
