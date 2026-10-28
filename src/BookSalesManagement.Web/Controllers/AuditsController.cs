using BookSalesManagement.Application.Abstractions;
using BookSalesManagement.Application.Common;
using BookSalesManagement.Application.Dtos;
using BookSalesManagement.Application.Services;
using BookSalesManagement.Domain.Common;
using BookSalesManagement.Web.Infrastructure;
using BookSalesManagement.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace BookSalesManagement.Web.Controllers;

[RequirePermission(PermissionCodes.PerformInventoryAudit, PermissionCodes.ApproveInventoryAudit)]
public sealed class AuditsController : Controller
{
    private readonly InventoryAuditService _service;
    private readonly IBookDirectory _books;
    private readonly IPermissionChecker _permissions;
    private readonly ICurrentUser _currentUser;

    public AuditsController(
        InventoryAuditService service,
        IBookDirectory books,
        IPermissionChecker permissions,
        ICurrentUser currentUser)
    {
        _service = service;
        _books = books;
        _permissions = permissions;
        _currentUser = currentUser;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
        => View(await _service.ListAsync(cancellationToken));

    [HttpGet]
    [RequirePermission(PermissionCodes.PerformInventoryAudit)]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
        => View(await BuildFormAsync(cancellationToken));

    [HttpPost]
    [RequirePermission(PermissionCodes.PerformInventoryAudit)]
    public async Task<IActionResult> Create(BookSelectionForm form, CancellationToken cancellationToken)
    {
        var selected = form.Books.Where(book => book.Selected).ToArray();
        if (selected.Length == 0)
        {
            ModelState.AddModelError(string.Empty, "Chọn ít nhất một sách để ghi nhận audit.");
        }

        foreach (var book in selected)
        {
            if (book.CountedQuantity is null || book.CountedQuantity < 0)
            {
                ModelState.AddModelError(string.Empty, $"Số lượng đếm của {book.Title} không hợp lệ.");
            }
        }

        if (!ModelState.IsValid)
        {
            await RepopulateAsync(form, cancellationToken);
            return View(form);
        }

        try
        {
            var lines = selected
                .Select(book => new NewCountLine(book.BookId, book.CountedQuantity!.Value))
                .ToArray();
            var auditId = await _service.CreateAsync(lines, cancellationToken);
            TempData["Success"] = "Đã lưu kết quả audit. Duyệt audit chỉ xác nhận kết quả, không thay đổi tồn kho.";
            return RedirectToAction(nameof(Details), new { id = auditId });
        }
        catch (DomainException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            await RepopulateAsync(form, cancellationToken);
            return View(form);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Details(string id, CancellationToken cancellationToken)
    {
        var audit = await _service.GetDetailsAsync(id, cancellationToken);
        if (audit is null)
        {
            return NotFound();
        }

        var employeeId = _currentUser.EmployeeId;
        var canApprove = !audit.IsApproved
                         && employeeId is not null
                         && await _permissions.HasPermissionAsync(employeeId, PermissionCodes.ApproveInventoryAudit, cancellationToken);

        return View(new AuditDetailsViewModel { Audit = audit, CanApprove = canApprove });
    }

    [HttpPost]
    [RequirePermission(PermissionCodes.ApproveInventoryAudit)]
    public async Task<IActionResult> Approve(string id, CancellationToken cancellationToken)
    {
        try
        {
            await _service.ApproveAsync(id, cancellationToken);
            TempData["Success"] = "Đã duyệt audit. Tồn kho và adjustment không thay đổi.";
        }
        catch (Exception exception) when (exception is DomainException or NotFoundException)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<BookSelectionForm> BuildFormAsync(CancellationToken cancellationToken)
    {
        var options = await _books.GetOptionsAsync(cancellationToken);
        return new BookSelectionForm
        {
            Books = options
                .Select(option => new BookEntryInput
                {
                    BookId = option.Id,
                    Title = option.Title,
                    CurrentQuantity = option.CurrentQuantity,
                    CountedQuantity = option.CurrentQuantity
                })
                .ToList()
        };
    }

    private async Task RepopulateAsync(BookSelectionForm form, CancellationToken cancellationToken)
    {
        var submitted = form.Books
            .Where(book => !string.IsNullOrWhiteSpace(book.BookId))
            .GroupBy(book => book.BookId)
            .ToDictionary(group => group.Key, group => group.First());

        var options = await _books.GetOptionsAsync(cancellationToken);
        form.Books = options
            .Select(option =>
            {
                submitted.TryGetValue(option.Id, out var input);
                return new BookEntryInput
                {
                    BookId = option.Id,
                    Title = option.Title,
                    CurrentQuantity = option.CurrentQuantity,
                    Selected = input?.Selected ?? false,
                    CountedQuantity = input?.CountedQuantity ?? option.CurrentQuantity
                };
            })
            .ToList();
    }
}
