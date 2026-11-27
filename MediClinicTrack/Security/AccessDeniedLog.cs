using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Authorization.Policy;

namespace MediClinicTrack.Security;

// US06-2: a refused request (HTTP 403) is recorded. It goes to the application log for now;
// US13 swaps in an implementation that writes to the audit table, without touching the
// handler below. Only the user ID and path are recorded: no email, no query string,
// since either could contain personal or patient details.
public record AccessDeniedEvent(
    DateTimeOffset At, string? UserId, string[] Roles, string Method, string Path, string RequiredRoles);

public interface IAccessDeniedLog
{
    Task RecordAsync(AccessDeniedEvent deniedEvent);
}

public class LoggerAccessDeniedLog : IAccessDeniedLog
{
    private static readonly EventId AccessDenied = new(4030, "AccessDenied");
    private readonly ILogger<LoggerAccessDeniedLog> _logger;

    public LoggerAccessDeniedLog(ILogger<LoggerAccessDeniedLog> logger) => _logger = logger;

    public Task RecordAsync(AccessDeniedEvent e)
    {
        _logger.LogWarning(AccessDenied,
            "Access denied: user {UserId} with roles [{Roles}] requested {Method} {Path}, which requires {RequiredRoles}.",
            e.UserId, string.Join(", ", e.Roles), e.Method, e.Path, e.RequiredRoles);
        return Task.CompletedTask;
    }
}

// Runs whenever the authorization middleware refuses a request. Records the refusal for a
// signed-in user, then lets the default handler respond (a 403; see Program.cs).
// Users who aren't signed in aren't "denied": they're sent to the login page.
public class AccessDeniedLoggingHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _default = new();
    private readonly IAccessDeniedLog _log;

    public AccessDeniedLoggingHandler(IAccessDeniedLog log) => _log = log;

    public async Task HandleAsync(
        RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Forbidden)
        {
            var user = context.User;
            var required = authorizeResult.AuthorizationFailure?.FailedRequirements
                .OfType<RolesAuthorizationRequirement>()
                .SelectMany(r => r.AllowedRoles)
                .Distinct()
                .ToArray() ?? Array.Empty<string>();

            await _log.RecordAsync(new AccessDeniedEvent(
                DateTimeOffset.UtcNow,
                user.FindFirstValue(ClaimTypes.NameIdentifier),
                user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray(),
                context.Request.Method,
                context.Request.Path.Value ?? "",
                required.Length > 0 ? string.Join(" or ", required) : "a different permission"));
        }

        await _default.HandleAsync(next, context, policy, authorizeResult);
    }
}
