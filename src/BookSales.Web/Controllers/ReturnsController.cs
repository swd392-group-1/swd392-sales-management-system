using BookSales.Application.Returns;
using BookSales.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace BookSales.Web.Controllers;

[Route("returns")]
public sealed class ReturnsController(
    IBookReturnService bookReturnService,
    ILogger<ReturnsController> logger) : Controller
{
    [HttpGet("search")]
    public async Task<IActionResult> Search(
        string? orderId,
        CancellationToken cancellationToken)
    {
        var model = new BookReturnSearchViewModel
        {
            OrderId = orderId?.Trim()
        };

        if (string.IsNullOrWhiteSpace(model.OrderId))
        {
            return View("Search", model);
        }

        if (model.OrderId.Length > 200)
        {
            model.Error = "Mã đơn hàng không hợp lệ.";
            return View("Search", model);
        }

        model.Order = await bookReturnService.GetOrderForReturnAsync(
            model.OrderId,
            cancellationToken);

        if (model.Order is null)
        {
            model.Error = "Không tìm thấy đơn hàng.";
        }

        return View("Search", model);
    }

    [HttpGet("create/{orderId}")]
    public async Task<IActionResult> Create(
        string orderId,
        CancellationToken cancellationToken)
    {
        var order = await bookReturnService.GetOrderForReturnAsync(
            orderId,
            cancellationToken);

        if (order is null)
        {
            return NotFound();
        }

        return View("Create", BuildCreateModel(order));
    }

    [HttpPost("create", Name = "Returns_CreatePost")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        BookReturnCreateViewModel model,
        CancellationToken cancellationToken)
    {
        model.Lines ??= new List<BookReturnLineInput>();

        if (!ModelState.IsValid)
        {
            return await ReturnToCreateFormAsync(model, cancellationToken);
        }

        var request = new BookReturnRequest(
            model.ReturnId,
            model.OriginalOrderId,
            model.IsCashRefundConfirmed,
            model.Lines
                .Where(line => line.ReturnQuantity != 0m)
                .Select(line => new BookReturnLineRequest(
                    line.OriginalOrderLineId,
                    line.ReturnQuantity,
                    model.IsBookNewConfirmed))
                .ToArray());

        try
        {
            var result = await bookReturnService.CreateReturnAsync(
                request,
                cancellationToken);

            if (result.IsSuccessful)
            {
                return RedirectToAction(
                    nameof(Receipt),
                    new { returnId = result.ReturnId });
            }

            model.Error = result.Error ?? "Không thể hoàn tất trả sách.";
        }
        catch (Exception exception)
            when (exception is not OperationCanceledException)
        {
            logger.LogError(
                exception,
                "Unable to process book return {ReturnId} for order {OrderId}.",
                model.ReturnId,
                model.OriginalOrderId);

            model.Error =
                "Không thể hoàn tất trả sách lúc này. " +
                "Dữ liệu chưa được xác nhận; vui lòng kiểm tra và thử lại.";
        }

        return await ReturnToCreateFormAsync(model, cancellationToken);
    }

    [HttpGet("history")]
    public async Task<IActionResult> History(
        string? orderId,
        CancellationToken cancellationToken)
    {
        if (orderId?.Length > 200)
        {
            return View("History", new BookReturnHistoryViewModel
            {
                OrderId = orderId,
                Returns = Array.Empty<BookReturnSummary>()
            });
        }

        var returns = await bookReturnService.GetReturnHistoryAsync(
            orderId,
            cancellationToken);

        return View("History", new BookReturnHistoryViewModel
        {
            OrderId = string.IsNullOrWhiteSpace(orderId) ? null : orderId.Trim(),
            Returns = returns
        });
    }

    [HttpGet("{returnId}/receipt")]
    public async Task<IActionResult> Receipt(
        string returnId,
        CancellationToken cancellationToken)
    {
        var result = await bookReturnService.GetReturnByIdAsync(
            returnId,
            cancellationToken);

        return result is null
            ? NotFound()
            : View("Receipt", result);
    }

    [HttpGet("{returnId}")]
    public async Task<IActionResult> Details(
        string returnId,
        CancellationToken cancellationToken)
    {
        var result = await bookReturnService.GetReturnByIdAsync(
            returnId,
            cancellationToken);

        return result is null
            ? NotFound()
            : View("Details", result);
    }

    private async Task<IActionResult> ReturnToCreateFormAsync(
        BookReturnCreateViewModel model,
        CancellationToken cancellationToken)
    {
        model.Order = await bookReturnService.GetOrderForReturnAsync(
            model.OriginalOrderId,
            cancellationToken);

        if (model.Order is null)
        {
            return NotFound();
        }

        if (model.Lines.Count == 0)
        {
            model.Lines = BuildCreateModel(model.Order).Lines;
        }

        return View("Create", model);
    }

    private static BookReturnCreateViewModel BuildCreateModel(
        ReturnableOrderDetails order)
    {
        return new BookReturnCreateViewModel
        {
            ReturnId = Guid.NewGuid().ToString("N"),
            OriginalOrderId = order.OrderId,
            Order = order,
            Lines = order.Lines.Select(line => new BookReturnLineInput
            {
                OriginalOrderLineId = line.OriginalOrderLineId
            }).ToList()
        };
    }
}
