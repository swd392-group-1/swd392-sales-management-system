using BookSales.Application.Access;

namespace BookSales.Web.Middleware;

/// <summary>
/// Records anonymous requests rejected by cookie authorization before MVC can run.
/// Authenticated checks and their outcomes are recorded by AccessController/AccessGuard.
/// </summary>
public sealed class AccessAuditMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IAccessLogWriter writer)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var protectedPath = path.Equals("/access/permissions", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/access/demo", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/access/logs", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/access/logout", StringComparison.OrdinalIgnoreCase);

        if (protectedPath && context.User.Identity?.IsAuthenticated != true)
        {
            await writer.WriteAsync(
                null,
                path,
                false,
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                context.RequestAborted);
        }

        await next(context);
    }
}
