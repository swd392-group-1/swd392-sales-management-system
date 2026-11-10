using System.Security.Claims;
using BookSales.Application.Access;
using BookSales.Domain.Access;
using BookSales.Infrastructure.Persistence;
using BookSales.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookSales.Web.Controllers;

[Authorize]
[Route("access")]
public sealed class AccessController(
    IAccessGuard guard,
    IPermissionChecker checker,
    IAccessLogWriter logger,
    StaffPermissionService changes,
    BookSalesDbContext db,
    IConfiguration config) : Controller
{
    private string? AccountId => User.FindFirstValue(ClaimTypes.NameIdentifier);
    private string Ip => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    private string PathForLog => Request.Path.Value ?? "/access";

    private Task<bool> CheckAsync(string configKey, CancellationToken ct) =>
        // Always invoke AccessGuard, including when a permission ID was not configured.
        // An unconfigured ID fails closed AND records a denied attempt.
        guard.AuthorizeAsync(AccountId, config[configKey] ?? string.Empty,
            PathForLog, Ip, ct);

    [HttpGet("demo")]
    public async Task<IActionResult> Demo(CancellationToken cancellationToken)
    {
        if (!await CheckAsync("Access:DemoPermissionId", cancellationToken))
            return Forbid();
        return Content("PASS: chức năng demo được phép truy cập.", "text/plain");
    }

    [HttpGet("permissions")]
    public async Task<IActionResult> Permissions(CancellationToken cancellationToken)
    {
        if (!await CheckAsync("Access:Uc12PermissionId", cancellationToken))
            return Forbid();

        var actor = await (
            from a in db.Accounts.AsNoTracking()
            join e in db.Employees.AsNoTracking() on a.EmployeeId equals e.EmployeeId
            where a.AccountId == AccountId && a.Status == "ACTIVE"
            select new { e.EmployeeId, e.RoleCode }
        ).SingleOrDefaultAsync(cancellationToken);

        if (actor is null) return Forbid();

        var employees = await db.Employees.AsNoTracking()
            .OrderBy(e => e.Name).ToListAsync(cancellationToken);
        var permissions = await db.Permissions.AsNoTracking()
            .OrderBy(p => p.PermissionId).ToListAsync(cancellationToken);
        var assignments = await db.EmployeePermissions.AsNoTracking()
            .ToListAsync(cancellationToken);

        if (actor.RoleCode == "STORE_MANAGER")
        {
            // Policy independently rejects self-edit and SYSTEM_ADMIN target.
            employees = employees.Where(e => e.EmployeeId != actor.EmployeeId
                                              && e.RoleCode != "SYSTEM_ADMIN").ToList();
            var editable = config.GetSection("Access:ManagerEditablePermissionIds")
                .Get<string[]>() ?? [];
            var targetRoles = config.GetSection("Access:ManagerEditableTargetRoleCodes")
                .Get<string[]>() ?? [];
            employees = employees.Where(e => targetRoles.Contains(e.RoleCode,
                StringComparer.Ordinal)).ToList();
            permissions = permissions.Where(p => editable.Contains(p.PermissionId,
                StringComparer.Ordinal)).ToList();
        }

        ViewBag.Employees = employees;
        ViewBag.Permissions = permissions;
        ViewBag.AllPermissions = await db.Permissions.AsNoTracking()
            .OrderBy(p => p.PermissionId).ToListAsync(cancellationToken);
        ViewBag.Assignments = assignments;
        ViewBag.ActorRole = actor.RoleCode;
        return View(new StaffPermissionInput());
    }

    [HttpPost("permissions")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Permissions(StaffPermissionInput input,
        CancellationToken cancellationToken)
    {
        // For a write, log the FINAL policy decision rather than logging
        // endpoint access as allowed before the business decision completes.
        var uc12Id = config["Access:Uc12PermissionId"];
        var canAccess = !string.IsNullOrWhiteSpace(AccountId)
            && !string.IsNullOrWhiteSpace(uc12Id)
            && await checker.HasPermissionAsync(AccountId, uc12Id, cancellationToken);

        if (!canAccess)
        {
            await logger.WriteAsync(AccountId, PathForLog, false, Ip, cancellationToken);
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            await logger.WriteAsync(AccountId, PathForLog, false, Ip, cancellationToken);
            return BadRequest("Invalid request.");
        }

        // SqlStaffPermissionStore resolves the same scoped DbContext. Its save and
        // the audit insert therefore join one SQL transaction: no silent grant on
        // audit failure. The denied policy result is logged in the same transaction.
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var changed = await changes.ChangeAsync(new StaffPermissionChange(
            AccountId!, input.TargetEmployeeId, input.PermissionId, input.Grant),
            cancellationToken);

        db.UserAccessLogs.Add(new UserAccessLog
        {
            AccountId = AccountId,
            OccurredAtUtc = DateTime.UtcNow,
            RequestPath = PathForLog,
            WasAuthorized = changed,
            ClientIp = Ip
        });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        TempData["AccessMessage"] = changed
            ? "Đã lưu thay đổi quyền."
            : "Không thể thực hiện thay đổi quyền theo chính sách hiện tại.";
        return RedirectToAction(nameof(Permissions));
    }

    [HttpGet("logs")]
    public async Task<IActionResult> Logs(CancellationToken cancellationToken)
    {
        var actor = await (
            from a in db.Accounts.AsNoTracking()
            join e in db.Employees.AsNoTracking() on a.EmployeeId equals e.EmployeeId
            where a.AccountId == AccountId && a.Status == "ACTIVE"
            select new { e.RoleCode }
        ).SingleOrDefaultAsync(cancellationToken);

        var isAdmin = actor?.RoleCode == "SYSTEM_ADMIN";
        var isManager = actor?.RoleCode == "STORE_MANAGER";
        await logger.WriteAsync(AccountId, PathForLog, isAdmin || isManager,
            Ip, cancellationToken);

        if (!isAdmin && !isManager) return Forbid();

        // Manager sees employee access activity, never SYSTEM_ADMIN's activity.
        // Anonymous attempts cannot be attributed to employees, so only Admin sees them.
        var query =
            from log in db.UserAccessLogs.AsNoTracking()
            join a in db.Accounts.AsNoTracking() on log.AccountId equals a.AccountId into la
            from a in la.DefaultIfEmpty()
            join e in db.Employees.AsNoTracking() on a.EmployeeId equals e.EmployeeId into ae
            from e in ae.DefaultIfEmpty()
            where isAdmin || (e != null && e.RoleCode != "SYSTEM_ADMIN")
            orderby log.OccurredAtUtc descending
            select new AccessLogRow(log.OccurredAtUtc,
                a != null ? a.Username : "(anonymous)", log.RequestPath,
                log.WasAuthorized, log.ClientIp);

        return View(await query.Take(100).ToListAsync(cancellationToken));
    }
}

public sealed record AccessLogRow(DateTime OccurredAtUtc, string Username,
    string Path, bool Allowed, string ClientIp);
