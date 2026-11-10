
namespace BookSales.Application.Access;

public interface IPermissionChecker
{
    Task<bool> HasPermissionAsync(
        string accountId,
        string permissionId,
        CancellationToken cancellationToken = default);
}
