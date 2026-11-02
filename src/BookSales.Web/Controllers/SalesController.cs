
using BookSales.Application.Sales;
using BookSales.Web.Models;
using BookSales.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace BookSales.Web.Controllers;

[Route("sales")]
public sealed class SalesController(
    ISaleProcessingService saleProcessingService,
    ILogger<SalesController> logger,
    QrCodeService qrCodeService,
    IWebHostEnvironment environment) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Create(
        CancellationToken cancellationToken)
    {
        var books = await saleProcessingService.GetBookOptionsAsync(
            cancellationToken);

        var model = new ProcessSaleViewModel
        {
            OrderId = Guid.NewGuid().ToString("N"),
            Books = books,
            Lines = books.Select(book =>
                new ProcessSaleLineInput
                {
                    BookId = book.BookId
                }).ToList(),
            Error = TempData["SaleError"] as string
        };

        return View(model);
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        ProcessSaleViewModel model,
        CancellationToken cancellationToken)
    {
        for (var index = 0; index < model.Lines.Count; index++)
        {
            var quantity = model.Lines[index].Quantity;

            if (quantity < 0 ||
                quantity != decimal.Truncate(quantity))
            {
                ModelState.AddModelError(
                    $"Lines[{index}].Quantity",
                    "Số lượng phải là số nguyên không âm.");
            }
        }

        if (model.PaymentMethod is not ("Cash" or "QR"))
        {
            ModelState.AddModelError(
                nameof(model.PaymentMethod),
                "Phương thức thanh toán không hợp lệ.");
        }

        if (!ModelState.IsValid)
        {
            return await ReturnToFormAsync(
                model, cancellationToken);
        }

        var request = new ProcessSaleRequest(
            model.OrderId,
            null,
            model.Lines
                .Where(line => line.Quantity > 0)
                .Select(line => new ProcessSaleLineRequest(
                    line.BookId,
                    line.Quantity))
                .ToArray());

        try
        {
            var result = model.PaymentMethod == "QR"
                ? await saleProcessingService.CreateQrPendingAsync(
                    request, cancellationToken)
                : await saleProcessingService.ProcessAsync(
                    request, cancellationToken);

            if (result.Status == ProcessSaleStatus.PaymentPending)
            {
                model.Result = result;

                var qrContent =
                    $"BOOKSALES|ORDER:{result.OrderId}|AMOUNT:{result.TotalAmount:0.00}";

                ViewBag.QrCodeBase64 =
                    qrCodeService.GenerateQrBase64(qrContent);

                return View("QrPaymentPending", model);
            }

            if (result.IsSuccessful)
            {
                model.Result = result;
                return View("Receipt", model);
            }

            model.Error = result.Error;
        }
        catch (Exception exception)
            when (exception is not OperationCanceledException)
        {
            logger.LogError(
                exception,
                "Unable to process sale {OrderId}.",
                model.OrderId);

            model.Error =
                "Không thể hoàn tất đơn bán hàng lúc này. " +
                "Vui lòng kiểm tra tồn kho và thử lại.";
        }

        return await ReturnToFormAsync(
            model, cancellationToken);
    }

    [HttpPost("confirm-qr")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmQr(
        string orderId,
        CancellationToken cancellationToken)
    {
        // Chỉ cho phép xác nhận QR mô phỏng trong Development.
        if (!environment.IsDevelopment())
        {
            return NotFound();
        }

        try
        {
            var result =
                await saleProcessingService.ConfirmQrPaymentAsync(
                    orderId,
                    cancellationToken);

            if (result.IsSuccessful)
            {
                var model = new ProcessSaleViewModel
                {
                    OrderId = result.OrderId,
                    Result = result
                };

                return View("Receipt", model);
            }

            TempData["SaleError"] = result.Error ??
                "Không thể xác nhận thanh toán QR.";
        }
        catch (Exception exception)
            when (exception is not OperationCanceledException)
        {
            logger.LogError(
                exception,
                "Unable to confirm QR payment for order {OrderId}.",
                orderId);

            TempData["SaleError"] =
                "Không thể xác nhận thanh toán. Vui lòng kiểm tra lại đơn hàng.";
        }

        return RedirectToAction(nameof(Create));
    }

    private async Task<IActionResult> ReturnToFormAsync(
        ProcessSaleViewModel model,
        CancellationToken cancellationToken)
    {
        model.Books = await saleProcessingService.GetBookOptionsAsync(
            cancellationToken);

        return View("Create", model);
    }
}
