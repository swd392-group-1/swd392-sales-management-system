using BookSales.Application.Returns;

namespace BookSales.Tests;

public sealed class BookReturnServiceTests
{
    [Theory]
    [InlineData("Cash")]
    [InlineData("QR")]
    public async Task CreateReturnAsync_AcceptsCompletedPaymentAndUsesCashRefund(
        string paymentMethod)
    {
        var repository = new FakeBookReturnRepository(
            CreateOrder(paymentMethod: paymentMethod));
        var service = new BookReturnService(repository);

        var result = await service.CreateReturnAsync(
            CreateRequest("return-1"),
            CancellationToken.None);

        Assert.Equal(BookReturnStatus.Completed, result.Status);
        Assert.Equal(paymentMethod, result.OriginalPaymentMethod);
        Assert.Equal("Cash", result.RefundMethod);
        Assert.Equal(20m, result.RefundAmount);
        Assert.Equal(1, repository.CreateCalls);
    }

    [Fact]
    public async Task CreateReturnAsync_RejectsOrderOutsideSevenDayWindow()
    {
        var repository = new FakeBookReturnRepository(
            CreateOrder(isWithinWindow: false));
        var result = await new BookReturnService(repository).CreateReturnAsync(
            CreateRequest("return-expired"),
            CancellationToken.None);

        Assert.Equal(BookReturnStatus.ReturnWindowExpired, result.Status);
        Assert.Equal(0, repository.CreateCalls);
    }

    [Fact]
    public async Task CreateReturnAsync_RejectsQuantityWithMoreThanTenFractionalDigits()
    {
        var repository = new FakeBookReturnRepository(CreateOrder());
        var result = await new BookReturnService(repository).CreateReturnAsync(
            CreateRequest("return-scale", 1.00000000001m),
            CancellationToken.None);

        Assert.Equal(BookReturnStatus.InvalidRequest, result.Status);
        Assert.Equal(0, repository.CreateCalls);
    }

    [Fact]
    public async Task CreateReturnAsync_RejectsUnpaidOrder()
    {
        var repository = new FakeBookReturnRepository(
            CreateOrder(paymentStatus: "Pending", isPaid: false));
        var result = await new BookReturnService(repository).CreateReturnAsync(
            CreateRequest("return-pending"),
            CancellationToken.None);

        Assert.Equal(BookReturnStatus.PaymentNotCompleted, result.Status);
        Assert.Equal(0, repository.CreateCalls);
    }

    [Fact]
    public async Task CreateReturnAsync_RejectsNonexistentOrder()
    {
        var repository = new FakeBookReturnRepository(null);
        var result = await new BookReturnService(repository).CreateReturnAsync(
            CreateRequest("return-missing"),
            CancellationToken.None);

        Assert.Equal(BookReturnStatus.OrderNotFound, result.Status);
        Assert.Equal(0, repository.CreateCalls);
    }

    [Fact]
    public async Task CreateReturnAsync_RejectsLineNotInOriginalOrder()
    {
        var repository = new FakeBookReturnRepository(CreateOrder());
        var request = new BookReturnRequest(
            "return-wrong-line",
            "order-1",
            true,
            new[] { new BookReturnLineRequest(99, 1m, true) });

        var result = await new BookReturnService(repository).CreateReturnAsync(
            request,
            CancellationToken.None);

        Assert.Equal(BookReturnStatus.OrderLineNotFound, result.Status);
        Assert.Equal(0, repository.CreateCalls);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CreateReturnAsync_RejectsNonPositiveQuantity(decimal quantity)
    {
        var repository = new FakeBookReturnRepository(CreateOrder());
        var request = CreateRequest("return-invalid-quantity", quantity);

        var result = await new BookReturnService(repository).CreateReturnAsync(
            request,
            CancellationToken.None);

        Assert.Equal(BookReturnStatus.InvalidRequest, result.Status);
        Assert.Equal(0, repository.CreateCalls);
    }

    [Fact]
    public async Task CreateReturnAsync_RejectsQuantityAboveRemainingAmount()
    {
        var repository = new FakeBookReturnRepository(
            CreateOrder(purchasedQuantity: 5m, previouslyReturned: 2m));
        var result = await new BookReturnService(repository).CreateReturnAsync(
            CreateRequest("return-too-many", 4m),
            CancellationToken.None);

        Assert.Equal(BookReturnStatus.QuantityExceedsRemaining, result.Status);
        Assert.Equal(0, repository.CreateCalls);
    }

    [Fact]
    public async Task CreateReturnAsync_AllowsMultiplePartialReturnsWithinPurchasedQuantity()
    {
        var repository = new FakeBookReturnRepository(
            CreateOrder(purchasedQuantity: 5m));
        var service = new BookReturnService(repository);

        var first = await service.CreateReturnAsync(
            CreateRequest("return-part-1", 2m),
            CancellationToken.None);
        var second = await service.CreateReturnAsync(
            CreateRequest("return-part-2", 3m),
            CancellationToken.None);

        Assert.Equal(BookReturnStatus.Completed, first.Status);
        Assert.Equal(BookReturnStatus.Completed, second.Status);
        Assert.Equal(2, repository.CreateCalls);
        Assert.Equal(5m, repository.ReturnedQuantity);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("0")]
    public async Task CreateReturnAsync_RejectsMissingOrZeroOriginalUnitPrice(
        string? unitPriceText)
    {
        decimal? unitPrice = unitPriceText is null
            ? null
            : decimal.Parse(unitPriceText);
        var repository = new FakeBookReturnRepository(
            CreateOrder(unitPrice: unitPrice));
        var result = await new BookReturnService(repository).CreateReturnAsync(
            CreateRequest("return-no-price"),
            CancellationToken.None);

        Assert.Equal(BookReturnStatus.OriginalPriceUnavailable, result.Status);
        Assert.Equal(0, repository.CreateCalls);
    }

    [Fact]
    public async Task CreateReturnAsync_RejectsMissingBookNewConfirmation()
    {
        var repository = new FakeBookReturnRepository(CreateOrder());
        var result = await new BookReturnService(repository).CreateReturnAsync(
            CreateRequest("return-not-new", confirmedNew: false),
            CancellationToken.None);

        Assert.Equal(BookReturnStatus.BookNotNew, result.Status);
        Assert.Equal(0, repository.CreateCalls);
    }

    [Fact]
    public async Task CreateReturnAsync_RejectsMissingCashRefundConfirmation()
    {
        var repository = new FakeBookReturnRepository(CreateOrder());
        var request = new BookReturnRequest(
            "return-no-cash-confirm",
            "order-1",
            false,
            new[] { new BookReturnLineRequest(1, 1m, true) });

        var result = await new BookReturnService(repository).CreateReturnAsync(
            request,
            CancellationToken.None);

        Assert.Equal(BookReturnStatus.InvalidRequest, result.Status);
        Assert.Equal(0, repository.CreateCalls);
    }

    [Fact]
    public async Task CreateReturnAsync_DuplicateSubmissionDoesNotCreateAnotherReturn()
    {
        var repository = new FakeBookReturnRepository(CreateOrder());
        var service = new BookReturnService(repository);
        var request = CreateRequest("return-retry");

        var first = await service.CreateReturnAsync(request, CancellationToken.None);
        var retry = await service.CreateReturnAsync(request, CancellationToken.None);

        Assert.Equal(BookReturnStatus.Completed, first.Status);
        Assert.Equal(BookReturnStatus.AlreadyProcessed, retry.Status);
        Assert.Equal(1, repository.CreateCalls);
    }

    [Fact]
    public async Task GetReturnByIdAsync_ReadsReceiptWithoutCreatingAnotherReturn()
    {
        var repository = new FakeBookReturnRepository(CreateOrder());
        var service = new BookReturnService(repository);
        var created = await service.CreateReturnAsync(
            CreateRequest("return-read-only"),
            CancellationToken.None);

        var receipt = await service.GetReturnByIdAsync(
            created.ReturnId,
            CancellationToken.None);

        Assert.NotNull(receipt);
        Assert.Equal(1, repository.CreateCalls);
    }

    private static ReturnableOrderDetails CreateOrder(
        string paymentMethod = "Cash",
        string paymentStatus = "Completed",
        bool isPaid = true,
        bool isWithinWindow = true,
        decimal purchasedQuantity = 4m,
        decimal previouslyReturned = 0m,
        decimal? unitPrice = 10m)
    {
        return new ReturnableOrderDetails(
            "order-1",
            new DateOnly(2026, 10, 27),
            100m,
            paymentMethod,
            paymentStatus,
            isPaid,
            isWithinWindow,
            null,
            new[]
            {
                new ReturnableOrderLine(
                    1,
                    "book-1",
                    "Book One",
                    purchasedQuantity,
                    previouslyReturned,
                    purchasedQuantity - previouslyReturned,
                    unitPrice)
            });
    }

    private static BookReturnRequest CreateRequest(
        string returnId,
        decimal quantity = 2m,
        bool confirmedNew = true)
    {
        return new BookReturnRequest(
            returnId,
            "order-1",
            true,
            new[] { new BookReturnLineRequest(1, quantity, confirmedNew) });
    }

    private sealed class FakeBookReturnRepository(
        ReturnableOrderDetails? order) : IBookReturnRepository
    {
        private readonly Dictionary<string, BookReturnResult> returns =
            new(StringComparer.OrdinalIgnoreCase);

        public int CreateCalls { get; private set; }
        public decimal ReturnedQuantity { get; private set; }

        public Task<ReturnableOrderDetails?> GetOrderForReturnAsync(
            string orderId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(
                order is not null &&
                string.Equals(order.OrderId, orderId, StringComparison.OrdinalIgnoreCase)
                    ? order
                    : null);
        }

        public Task<IReadOnlyList<BookReturnSummary>> GetReturnHistoryAsync(
            string? orderId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<BookReturnSummary>>(
                returns.Values.Select(item => new BookReturnSummary(
                    item.ReturnId,
                    item.OriginalOrderId,
                    item.ReturnedAtUtc ?? DateTime.UtcNow,
                    item.RefundAmount)).ToArray());
        }

        public Task<BookReturnResult?> GetReturnByIdAsync(
            string returnId,
            CancellationToken cancellationToken)
        {
            returns.TryGetValue(returnId, out var result);
            return Task.FromResult(result);
        }

        public Task<BookReturnResult> CreateReturnAsync(
            BookReturnRequest request,
            CancellationToken cancellationToken)
        {
            CreateCalls++;
            var returnLines = request.Lines.Select(line =>
            {
                var original = order!.Lines.Single(item =>
                    item.OriginalOrderLineId == line.OriginalOrderLineId);
                var refund = line.ReturnQuantity * original.OriginalUnitPrice!.Value;
                return new BookReturnLineResult(
                    original.OriginalOrderLineId,
                    original.BookId,
                    original.Title,
                    line.ReturnQuantity,
                    original.OriginalUnitPrice.Value,
                    refund);
            }).ToArray();
            var result = new BookReturnResult(
                BookReturnStatus.Completed,
                request.ReturnId,
                request.OriginalOrderId,
                DateTime.UtcNow,
                order!.PaymentMethod,
                returnLines,
                returnLines.Sum(line => line.RefundAmount),
                null);
            returns.Add(request.ReturnId, result);
            ReturnedQuantity += returnLines.Sum(line => line.ReturnedQuantity);
            order = order with
            {
                Lines = order.Lines.Select(line =>
                {
                    var newlyReturned = returnLines
                        .Where(returnLine => returnLine.OriginalOrderLineId == line.OriginalOrderLineId)
                        .Sum(returnLine => returnLine.ReturnedQuantity);
                    var previous = line.PreviouslyReturnedQuantity + newlyReturned;
                    return line with
                    {
                        PreviouslyReturnedQuantity = previous,
                        RemainingQuantity = Math.Max(0m, line.PurchasedQuantity - previous)
                    };
                }).ToArray()
            };

            return Task.FromResult(result);
        }
    }
}
