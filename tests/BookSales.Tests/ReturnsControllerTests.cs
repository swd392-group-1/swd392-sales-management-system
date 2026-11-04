using BookSales.Application.Returns;
using BookSales.Web.Controllers;
using BookSales.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace BookSales.Tests;

public sealed class ReturnsControllerTests
{
    [Fact]
    public async Task Receipt_IsReadOnlyAndDoesNotCreateAnotherReturn()
    {
        var service = new ReadOnlyReturnService();
        var controller = new ReturnsController(
            service,
            NullLogger<ReturnsController>.Instance);

        var result = await controller.Receipt(
            "return-1",
            CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("Receipt", view.ViewName);
        Assert.Equal(0, service.CreateCalls);
    }

    [Fact]
    public async Task Create_PostMapsGlobalConfirmationsAndRedirectsToReceipt()
    {
        var service = new CapturingReturnService(new BookReturnResult(
            BookReturnStatus.Completed,
            "return-1",
            "order-1",
            DateTime.UtcNow,
            "Cash",
            Array.Empty<BookReturnLineResult>(),
            0m,
            null));
        var controller = new ReturnsController(
            service,
            NullLogger<ReturnsController>.Instance);
        var model = new BookReturnCreateViewModel
        {
            ReturnId = "return-1",
            OriginalOrderId = "order-1",
            IsBookNewConfirmed = true,
            IsCashRefundConfirmed = true,
            Lines = new List<BookReturnLineInput>
            {
                new() { OriginalOrderLineId = 11, ReturnQuantity = 1m },
                new() { OriginalOrderLineId = 12, ReturnQuantity = 0m }
            }
        };

        var result = await controller.Create(model, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(ReturnsController.Receipt), redirect.ActionName);
        Assert.Equal("return-1", redirect.RouteValues!["returnId"]);
        var request = Assert.IsType<BookReturnRequest>(service.Request);
        Assert.True(request.IsCashRefundConfirmed);
        var line = Assert.Single(request.Lines);
        Assert.Equal(11, line.OriginalOrderLineId);
        Assert.True(line.IsBookNewConfirmed);
    }

    [Fact]
    public async Task Create_PostReturnsToFormWithServiceErrorWhenBookIsNotConfirmed()
    {
        const string error = "Nhân viên bán hàng ph?i xác nh?n sách còn m?i tr??c khi hoàn t?t tr? hàng.";
        var service = new CapturingReturnService(new BookReturnResult(
            BookReturnStatus.BookNotNew,
            "return-1",
            "order-1",
            null,
            null,
            Array.Empty<BookReturnLineResult>(),
            0m,
            error));
        var controller = new ReturnsController(
            service,
            NullLogger<ReturnsController>.Instance);
        var model = new BookReturnCreateViewModel
        {
            ReturnId = "return-1",
            OriginalOrderId = "order-1",
            IsCashRefundConfirmed = true,
            Lines = new List<BookReturnLineInput>
            {
                new() { OriginalOrderLineId = 11, ReturnQuantity = 1m }
            }
        };

        var result = await controller.Create(model, CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("Create", view.ViewName);
        Assert.Equal(error, Assert.IsType<BookReturnCreateViewModel>(view.Model).Error);
        var request = Assert.IsType<BookReturnRequest>(service.Request);
        Assert.False(Assert.Single(request.Lines).IsBookNewConfirmed);
    }

    private sealed class ReadOnlyReturnService : IBookReturnService
    {
        private static readonly BookReturnResult ReceiptResult = new(
            BookReturnStatus.Completed,
            "return-1",
            "order-1",
            DateTime.UtcNow,
            "QR",
            Array.Empty<BookReturnLineResult>(),
            0m,
            null);

        public int CreateCalls { get; private set; }

        public Task<ReturnableOrderDetails?> GetOrderForReturnAsync(
            string orderId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<ReturnableOrderDetails?>(null);
        }

        public Task<IReadOnlyList<BookReturnSummary>> GetReturnHistoryAsync(
            string? orderId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<BookReturnSummary>>(
                Array.Empty<BookReturnSummary>());
        }

        public Task<BookReturnResult?> GetReturnByIdAsync(
            string returnId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<BookReturnResult?>(ReceiptResult);
        }

        public Task<BookReturnResult> CreateReturnAsync(
            BookReturnRequest request,
            CancellationToken cancellationToken)
        {
            CreateCalls++;
            return Task.FromResult(ReceiptResult);
        }
    }

    private sealed class CapturingReturnService(BookReturnResult result) : IBookReturnService
    {
        public BookReturnRequest? Request { get; private set; }

        public Task<ReturnableOrderDetails?> GetOrderForReturnAsync(
            string orderId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<ReturnableOrderDetails?>(new ReturnableOrderDetails(
                orderId,
                new DateOnly(2026, 10, 27),
                100m,
                "Cash",
                "Completed",
                true,
                true,
                null,
                new[]
                {
                    new ReturnableOrderLine(11, "book-1", "Book One", 2m, 0m, 2m, 10m)
                }));
        }

        public Task<IReadOnlyList<BookReturnSummary>> GetReturnHistoryAsync(
            string? orderId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<BookReturnSummary>>(
                Array.Empty<BookReturnSummary>());
        }

        public Task<BookReturnResult?> GetReturnByIdAsync(
            string returnId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<BookReturnResult?>(null);
        }

        public Task<BookReturnResult> CreateReturnAsync(
            BookReturnRequest request,
            CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(result);
        }
    }
}
