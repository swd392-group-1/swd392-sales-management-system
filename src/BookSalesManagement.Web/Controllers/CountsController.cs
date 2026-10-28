using BookSalesManagement.Application.Abstractions;
using BookSalesManagement.Application.Common;
using BookSalesManagement.Application.Dtos;
using BookSalesManagement.Application.Services;
using BookSalesManagement.Domain.Common;
using BookSalesManagement.Web.Infrastructure;
using BookSalesManagement.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace BookSalesManagement.Web.Controllers;

[RequirePermission(PermissionCodes.RecordInventoryCount)]
public sealed class CountsController : Controller
{
    private readonly InventoryCountService _service;
    private readonly IBookDirectory _books;

    public CountsController(InventoryCountService service, IBookDirectory books)
    {
        _service = service;
        _books = books;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
        => View(await _service.ListAsync(cancellationToken));

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
        => View(await BuildFormAsync(cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create(BookSelectionForm form, CancellationToken cancellationToken)
    {
        var selected = form.Books.Where(book => book.Selected).ToArray();
        if (selected.Length == 0)
        {
            ModelState.AddModelError(string.Empty, "Chọn ít nhất một sách để kiểm kê.");
        }

        foreach (var book in selected)
        {
            if (book.CountedQuantity is null)
            {
                ModelState.AddModelError(string.Empty, $"Chưa nhập số lượng đếm cho {book.Title}.");
            }
            else if (book.CountedQuantity < 0)
            {
                ModelState.AddModelError(string.Empty, $"Số lượng đếm của {book.Title} không được âm.");
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
            var countId = await _service.CreateAsync(lines, cancellationToken);
            TempData["Success"] = "Đã tạo phiếu kiểm kê. Ghi thêm kết quả đếm nếu cần, sau đó chọn kết quả và chốt phiếu.";
            return RedirectToAction(nameof(Details), new { id = countId });
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
        var detail = await _service.GetDetailsAsync(id, cancellationToken);
        if (detail is null)
        {
            return NotFound();
        }

        var countBooks = detail.Lines
            .Select(line => new BookOption(line.BookId, line.BookTitle, line.CurrentQuantity))
            .ToArray();

        return View(new CountDetailsViewModel { Count = detail, CountBooks = countBooks });
    }

    [HttpPost]
    public async Task<IActionResult> AddObservation(AddObservationForm form, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(form.BookId))
        {
            TempData["Error"] = "Chọn sách cần ghi nhận kết quả đếm.";
            return RedirectToAction(nameof(Details), new { id = form.CountId });
        }

        if (form.CountedQuantity is null || form.CountedQuantity < 0)
        {
            TempData["Error"] = "Số lượng đếm phải là số không âm.";
            return RedirectToAction(nameof(Details), new { id = form.CountId });
        }

        try
        {
            await _service.AddObservationAsync(form.CountId, form.BookId, form.CountedQuantity.Value, cancellationToken);
            TempData["Success"] = "Đã ghi thêm một kết quả đếm. Chỉ kết quả được chọn khi chốt phiếu mới được dùng.";
        }
        catch (Exception exception) when (exception is DomainException or NotFoundException)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToAction(nameof(Details), new { id = form.CountId });
    }

    [HttpPost]
    public async Task<IActionResult> Finalize(FinalizeCountForm form, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.FinalizeAsync(form.CountId, form.Selections, cancellationToken);
            TempData["Success"] = result.AdjustmentId is null
                ? "Đã chốt phiếu. Tất cả dòng khớp tồn nên không tạo adjustment."
                : $"Đã chốt phiếu và tạo adjustment {result.AdjustmentId} cho các dòng lệch.";
        }
        catch (Exception exception) when (exception is DomainException or NotFoundException or ConflictException)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToAction(nameof(Details), new { id = form.CountId });
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
