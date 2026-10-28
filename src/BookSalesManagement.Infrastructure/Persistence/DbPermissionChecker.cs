using BookSalesManagement.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace BookSalesManagement.Infrastructure.Persistence;

public sealed class DbPermissionChecker : IPermissionChecker
{
    private readonly BookSalesDbContext _db;

    public DbPermissionChecker(BookSalesDbContext db)
    {
        _db = db;
    }

    public async Task<bool> HasPermissionAsync(
        string employeeId,
        string permissionId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(employeeId) || string.IsNullOrWhiteSpace(permissionId))
        {
            return false;
        }

        var employee = await _db.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == employeeId, cancellationToken);

        if (employee is null)
        {
            return false;
        }

        var hasActiveAccount = await _db.Accounts
            .AsNoTracking()
            .AnyAsync(account => account.EmployeeId == employeeId && account.Status == "ACTIVE", cancellationToken);

        if (!hasActiveAccount)
        {
            return false;
        }

        if (string.Equals(employee.RoleCode, "SYSTEM_ADMIN", StringComparison.Ordinal))
        {
            return true;
        }

        return await _db.EmployeePermissions
            .AsNoTracking()
            .AnyAsync(
                link => link.EmployeeId == employeeId && link.PermissionId == permissionId,
                cancellationToken);
    }
}
