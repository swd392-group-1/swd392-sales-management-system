
namespace BookSales.Application.Access;

public interface IAccessGuard
{
    Task<bool> AuthorizeAsync(
        string? accountId,
        string permissionId,
        string requestPath,
        string clientIp,
        CancellationToken cancellationToken = default);
}
