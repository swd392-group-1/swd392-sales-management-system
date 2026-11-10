using BookSales.Application.Access;
using BookSales.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BookSales.Infrastructure.Access;

public sealed class SqlStaffPermissionChangePolicy(
    BookSalesDbContext db,
    IConfiguration configuration) : IStaffPermissionChangePolicy
{
    public async Task<bool> IsAllowedAsync(
        StaffPermissionChange change,
        CancellationToken cancellationToken = default)
    {
        var actor = await (
            from a in db.Accounts.AsNoTracking()
            join e in db.Employees.AsNoTracking()
                on a.EmployeeId equals e.EmployeeId
            where a.AccountId == change.ActorAccountId
            select new { a.Status, e.EmployeeId, e.RoleCode }
        ).SingleOrDefaultAsync(cancellationToken);

        if (actor is null || actor.Status != "ACTIVE") return false;

        var target = await db.Employees.AsNoTracking()
            .Where(e => e.EmployeeId == change.TargetEmployeeId)
            .Select(e => new { e.EmployeeId, e.RoleCode })
            .SingleOrDefaultAsync(cancellationToken);
        if (target is null) return false;

        if (!await db.Permissions.AsNoTracking()
                .AnyAsync(p => p.PermissionId == change.PermissionId, cancellationToken))
            return false;

        if (actor.RoleCode == "SYSTEM_ADMIN") return true;
        if (actor.RoleCode != "STORE_MANAGER") return false;
        if (actor.EmployeeId == target.EmployeeId) return false;
        if (target.RoleCode == "SYSTEM_ADMIN") return false;

        // Manager's editable permissions AND eligible target roles still require
        // explicit group approval. An empty list means deny, never allow all.
        var editableIds = configuration.GetSection("Access:ManagerEditablePermissionIds")
            .Get<string[]>() ?? [];
        var targetRoles = configuration.GetSection("Access:ManagerEditableTargetRoleCodes")
            .Get<string[]>() ?? [];
        if (!editableIds.Contains(change.PermissionId, StringComparer.Ordinal)
            || !targetRoles.Contains(target.RoleCode, StringComparer.Ordinal))
            return false;

        var uc12Id = configuration["Access:Uc12PermissionId"];
        if (string.IsNullOrWhiteSpace(uc12Id)) return false;
        return await db.EmployeePermissions.AsNoTracking()
            .AnyAsync(p => p.EmployeeId == actor.EmployeeId
                        && p.PermissionId == uc12Id, cancellationToken);
    }
}
