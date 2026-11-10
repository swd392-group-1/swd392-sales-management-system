using BookSales.Application.Access;
using BookSales.Domain.Access;
using BookSales.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BookSales.Infrastructure.Access;

public sealed class SqlStaffPermissionStore(BookSalesDbContext db)
    : IStaffPermissionStore
{
    public async Task ApplyAsync(
        string employeeId,
        string permissionId,
        bool grant,
        CancellationToken cancellationToken = default)
    {
        var existing = await db.EmployeePermissions.FindAsync(
            new object[] { employeeId, permissionId }, cancellationToken);

        if (grant && existing is null)
        {
            db.EmployeePermissions.Add(new EmployeePermission
            {
                EmployeeId = employeeId,
                PermissionId = permissionId
            });
        }
        else if (!grant && existing is not null)
        {
            db.EmployeePermissions.Remove(existing);
        }
        else return; // Idempotent assignment/removal.

        await db.SaveChangesAsync(cancellationToken);
    }
}
