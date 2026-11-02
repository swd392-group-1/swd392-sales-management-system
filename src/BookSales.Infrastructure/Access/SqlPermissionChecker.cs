
using BookSales.Application.Access;
using BookSales.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BookSales.Infrastructure.Access;

public sealed class SqlPermissionChecker(
    BookSalesDbContext db) : IPermissionChecker
{
    public async Task<bool> HasPermissionAsync(
        string accountId,
        string permissionId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(accountId) ||
            string.IsNullOrWhiteSpace(permissionId))
        {
            return false;
        }

        var account = await (
            from a in db.Accounts.AsNoTracking()
            join e in db.Employees.AsNoTracking()
                on a.EmployeeId equals e.EmployeeId
            where a.AccountId == accountId
            select new
            {
                a.Status,
                e.EmployeeId,
                e.RoleCode
            }
        ).SingleOrDefaultAsync(cancellationToken);

        if (account is null || account.Status != "ACTIVE")
        {
            return false;
        }

        if (account.RoleCode == "SYSTEM_ADMIN")
        {
            return await db.Permissions
                .AsNoTracking()
                .AnyAsync(
                    p => p.PermissionId == permissionId,
                    cancellationToken);
        }

        return await db.EmployeePermissions
            .AsNoTracking()
            .AnyAsync(
                ep => ep.EmployeeId == account.EmployeeId
                   && ep.PermissionId == permissionId,
                cancellationToken);
    }
}
