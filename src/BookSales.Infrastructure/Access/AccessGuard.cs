
using BookSales.Application.Access;

namespace BookSales.Infrastructure.Access;

public sealed class AccessGuard(
    IPermissionChecker permissionChecker,
    IAccessLogWriter accessLogWriter) : IAccessGuard
{
    public async Task<bool> AuthorizeAsync(
        string? accountId,
        string permissionId,
        string requestPath,
        string clientIp,
        CancellationToken cancellationToken = default)
    {
        bool allowed = false;

        if (!string.IsNullOrWhiteSpace(accountId))
        {
            allowed = await permissionChecker.HasPermissionAsync(
                accountId,
                permissionId,
                cancellationToken);
        }

        await accessLogWriter.WriteAsync(
            accountId,
            requestPath,
            allowed,
            clientIp,
            cancellationToken);

        return allowed;
    }
}
