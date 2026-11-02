
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BookSales.Application.Sales;

namespace BookSales.Tests;

public sealed class ProcessSaleTests
{
    [Fact]
    public async Task ProcessAsync_MergesRepeatedBookLinesBeforeCallingRepository()
    {
        var repository = new FakeSaleRepository();
        var service = new SaleProcessingService(repository);

        var request = new ProcessSaleRequest(
            "order-1",
            null,
            new[]
            {
                new ProcessSaleLineRequest("book-1", 1m),
                new ProcessSaleLineRequest("BOOK-1", 2m),
                new ProcessSaleLineRequest("book-2", 3m)
            });

        var result = await service.ProcessAsync(
            request,
            CancellationToken.None);

        Assert.Equal(ProcessSaleStatus.Completed, result.Status);
        Assert.NotNull(repository.LastRequest);
        Assert.Equal(2, repository.LastRequest.Lines.Count);

        Assert.Equal(
            3m,
            repository.LastRequest.Lines.Single(
                line => line.BookId == "book-1").Quantity);

        Assert.Equal(
            3m,
            repository.LastRequest.Lines.Single(
                line => line.BookId == "book-2").Quantity);
    }

    [Fact]
    public async Task ProcessAsync_RejectsNonPositiveOrFractionalQuantityWithoutCallingRepository()
    {
        foreach (var quantity in new[] { 0m, -1m, 1.5m })
        {
            var repository = new FakeSaleRepository();
            var service = new SaleProcessingService(repository);

            var request = new ProcessSaleRequest(
                "order-1",
                null,
                new[]
                {
                    new ProcessSaleLineRequest("book-1", quantity)
                });

            var result = await service.ProcessAsync(
                request,
                CancellationToken.None);

            Assert.Equal(ProcessSaleStatus.InvalidRequest, result.Status);
            Assert.Null(repository.LastRequest);
        }
    }

    [Fact]
    public async Task ProcessAsync_RejectsOrderWithoutPurchasedBooks()
    {
        var repository = new FakeSaleRepository();
        var service = new SaleProcessingService(repository);

        var request = new ProcessSaleRequest(
            "order-1",
            null,
            Array.Empty<ProcessSaleLineRequest>());

        var result = await service.ProcessAsync(
            request,
            CancellationToken.None);

        Assert.Equal(ProcessSaleStatus.InvalidRequest, result.Status);

        Assert.Equal(
            "Đơn hàng phải có ít nhất một đầu sách.",
            result.Error);

        Assert.Null(repository.LastRequest);
    }

    [Fact]
    public async Task CreateQrPendingAsync_MergesRepeatedBookLinesAndUsesQrRepositoryOperation()
    {
        var repository = new FakeSaleRepository();
        var service = new SaleProcessingService(repository);

        var request = new ProcessSaleRequest(
            "order-qr-1",
            null,
            new[]
            {
                new ProcessSaleLineRequest("book-1", 1m),
                new ProcessSaleLineRequest("BOOK-1", 2m)
            });

        var result = await service.CreateQrPendingAsync(
            request,
            CancellationToken.None);

        Assert.Equal(ProcessSaleStatus.PaymentPending, result.Status);
        Assert.Equal("QR", result.PaymentMethod);
        Assert.Equal("Pending", result.PaymentStatus);

        Assert.NotNull(repository.LastQrRequest);
        Assert.Single(repository.LastQrRequest.Lines);

        Assert.Equal(
            3m,
            repository.LastQrRequest.Lines[0].Quantity);

        Assert.Null(repository.LastRequest);
    }

    [Fact]
    public async Task CreateQrPendingAsync_RejectsInvalidQuantityWithoutCallingRepository()
    {
        var repository = new FakeSaleRepository();
        var service = new SaleProcessingService(repository);

        var request = new ProcessSaleRequest(
            "order-qr-2",
            null,
            new[]
            {
                new ProcessSaleLineRequest("book-1", 1.5m)
            });

        var result = await service.CreateQrPendingAsync(
            request,
            CancellationToken.None);

        Assert.Equal(ProcessSaleStatus.InvalidRequest, result.Status);
        Assert.Null(repository.LastQrRequest);
    }

    [Fact]
    public async Task ConfirmQrPaymentAsync_CallsRepositoryWithOrderId()
    {
        var repository = new FakeSaleRepository();
        var service = new SaleProcessingService(repository);

        var result = await service.ConfirmQrPaymentAsync(
            "order-qr-3",
            CancellationToken.None);

        Assert.Equal(ProcessSaleStatus.Completed, result.Status);
        Assert.Equal("order-qr-3", repository.LastConfirmedOrderId);
        Assert.Equal("QR", result.PaymentMethod);
        Assert.Equal("Completed", result.PaymentStatus);
    }

    private sealed class FakeSaleRepository : ISaleRepository
    {
        public ProcessSaleRequest? LastRequest { get; private set; }

        public ProcessSaleRequest? LastQrRequest { get; private set; }

        public string? LastConfirmedOrderId { get; private set; }

        public Task<IReadOnlyList<SaleBookOption>> GetBookOptionsAsync(
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<SaleBookOption>>(
                Array.Empty<SaleBookOption>());
        }

        public Task<ProcessSaleResult> ProcessAsync(
            ProcessSaleRequest request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;

            return Task.FromResult(
                new ProcessSaleResult(
                    ProcessSaleStatus.Completed,
                    request.OrderId,
                    0m,
                    Array.Empty<ProcessSaleLineResult>(),
                    null,
                    "Cash",
                    "Completed"));
        }

        public Task<ProcessSaleResult> CreateQrPendingAsync(
            ProcessSaleRequest request,
            CancellationToken cancellationToken)
        {
            LastQrRequest = request;

            return Task.FromResult(
                new ProcessSaleResult(
                    ProcessSaleStatus.PaymentPending,
                    request.OrderId,
                    0m,
                    Array.Empty<ProcessSaleLineResult>(),
                    null,
                    "QR",
                    "Pending"));
        }

        public Task<ProcessSaleResult> ConfirmQrPaymentAsync(
            string orderId,
            CancellationToken cancellationToken)
        {
            LastConfirmedOrderId = orderId;

            return Task.FromResult(
                new ProcessSaleResult(
                    ProcessSaleStatus.Completed,
                    orderId,
                    0m,
                    Array.Empty<ProcessSaleLineResult>(),
                    null,
                    "QR",
                    "Completed"));
        }
    }
}
