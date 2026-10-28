using BookSalesManagement.Application.Abstractions;
using BookSalesManagement.Application.Common;
using BookSalesManagement.Application.Dtos;
using BookSalesManagement.Application.Services;
using BookSalesManagement.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace BookSalesManagement.Web.Controllers;

public sealed class HomeController : Controller
{
    private readonly IInventoryQueryService _queries;
    private readonly InventoryCountService _counts;
    private readonly InventoryAuditService _audits;
    private readonly IPermissionChecker _permissions;
    private readonly ICurrentUser _currentUser;

    public HomeController(
        IInventoryQueryService queries,
        InventoryCountService counts,
        InventoryAuditService audits,
        IPermissionChecker permissions,
        ICurrentUser currentUser)
    {
        _queries = queries;
        _counts = counts;
        _audits = audits;
        _permissions = permissions;
        _currentUser = currentUser;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var employeeId = _currentUser.EmployeeId;
        var canCount = employeeId is not null
                       && await _permissions.HasPermissionAsync(employeeId, PermissionCodes.RecordInventoryCount, cancellationToken);
        var canAudit = employeeId is not null
                       && (await _permissions.HasPermissionAsync(employeeId, PermissionCodes.PerformInventoryAudit, cancellationToken)
                           || await _permissions.HasPermissionAsync(employeeId, PermissionCodes.ApproveInventoryAudit, cancellationToken));

        var stock = await _queries.GetStockOverviewAsync(cancellationToken);
        var movements = await _queries.GetRecentMovementsAsync(12, cancellationToken);
        IReadOnlyList<InventoryCountSummary> counts = canCount
            ? (await _counts.ListAsync(cancellationToken)).Take(5).ToArray()
            : Array.Empty<InventoryCountSummary>();
        IReadOnlyList<InventoryAuditSummary> audits = canAudit
            ? (await _audits.ListAsync(cancellationToken)).Take(5).ToArray()
            : Array.Empty<InventoryAuditSummary>();

        return View(new DashboardViewModel(stock, movements, counts, audits, canCount, canAudit));
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new Models.ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
}
