namespace BookSales.Application.Returns;

public sealed class BookReturnService(
    IBookReturnRepository repository) : IBookReturnService
{
    public Task<ReturnableOrderDetails?> GetOrderForReturnAsync(
        string orderId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(orderId) || orderId.Length > 200)
        {
            return Task.FromResult<ReturnableOrderDetails?>(null);
        }

        return repository.GetOrderForReturnAsync(
            orderId.Trim(),
            cancellationToken);
    }

    public Task<IReadOnlyList<BookReturnSummary>> GetReturnHistoryAsync(
        string? orderId,
        CancellationToken cancellationToken)
    {
        var normalizedOrderId = string.IsNullOrWhiteSpace(orderId)
            ? null
            : orderId.Trim();

        if (normalizedOrderId?.Length > 200)
        {
            return Task.FromResult<IReadOnlyList<BookReturnSummary>>(
                Array.Empty<BookReturnSummary>());
        }

        return repository.GetReturnHistoryAsync(
            normalizedOrderId,
            cancellationToken);
    }

    public Task<BookReturnResult?> GetReturnByIdAsync(
        string returnId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(returnId) || returnId.Length > 200)
        {
            return Task.FromResult<BookReturnResult?>(null);
        }

        return repository.GetReturnByIdAsync(
            returnId.Trim(),
            cancellationToken);
    }

    public async Task<BookReturnResult> CreateReturnAsync(
        BookReturnRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null ||
            string.IsNullOrWhiteSpace(request.ReturnId) ||
            request.ReturnId.Length > 200 ||
            string.IsNullOrWhiteSpace(request.OriginalOrderId) ||
            request.OriginalOrderId.Length > 200 ||
            !request.IsCashRefundConfirmed ||
            request.Lines is null ||
            request.Lines.Count == 0)
        {
            return Failure(
                request?.ReturnId,
                request?.OriginalOrderId,
                BookReturnStatus.InvalidRequest,
                "Thông tin trả sách không hợp lệ hoặc chưa xác nhận hoàn tiền mặt.");
        }

        var normalizedRequest = new BookReturnRequest(
            request.ReturnId.Trim(),
            request.OriginalOrderId.Trim(),
            request.IsCashRefundConfirmed,
            request.Lines.ToArray());

        var lineIds = new HashSet<long>();
        foreach (var line in normalizedRequest.Lines)
        {
            if (line.OriginalOrderLineId <= 0 ||
                line.ReturnQuantity <= 0m ||
                line.ReturnQuantity != decimal.Round(line.ReturnQuantity, 10) ||
                !lineIds.Add(line.OriginalOrderLineId))
            {
                return Failure(
                    normalizedRequest.ReturnId,
                    normalizedRequest.OriginalOrderId,
                    BookReturnStatus.InvalidRequest,
                    "Mỗi dòng trả phải có mã hợp lệ và số lượng lớn hơn 0.");
            }

            if (!line.IsBookNewConfirmed)
            {
                return Failure(
                    normalizedRequest.ReturnId,
                    normalizedRequest.OriginalOrderId,
                    BookReturnStatus.BookNotNew,
                    "Nhân viên bán hàng phải xác nhận sách còn mới trước khi hoàn tất trả hàng.");
            }
        }

        var existingReturn = await repository.GetReturnByIdAsync(
            normalizedRequest.ReturnId,
            cancellationToken);

        if (existingReturn is not null)
        {
            var sameRequest = string.Equals(
                    existingReturn.OriginalOrderId,
                    normalizedRequest.OriginalOrderId,
                    StringComparison.OrdinalIgnoreCase) &&
                existingReturn.Lines.Count == normalizedRequest.Lines.Count &&
                existingReturn.Lines.All(existingLine =>
                    normalizedRequest.Lines.Any(requestedLine =>
                        requestedLine.OriginalOrderLineId == existingLine.OriginalOrderLineId &&
                        requestedLine.ReturnQuantity == existingLine.ReturnedQuantity));

            return sameRequest
                ? existingReturn with
                {
                    Status = BookReturnStatus.AlreadyProcessed,
                    Error = null
                }
                : Failure(
                    normalizedRequest.ReturnId,
                    normalizedRequest.OriginalOrderId,
                    BookReturnStatus.Conflict,
                    "Mã trả hàng đã được sử dụng cho nội dung khác.");
        }

        var order = await repository.GetOrderForReturnAsync(
            normalizedRequest.OriginalOrderId,
            cancellationToken);

        if (order is null)
        {
            return Failure(
                normalizedRequest.ReturnId,
                normalizedRequest.OriginalOrderId,
                BookReturnStatus.OrderNotFound,
                "Không tìm thấy đơn hàng gốc.");
        }

        if (!order.IsPaid)
        {
            return Failure(
                normalizedRequest.ReturnId,
                normalizedRequest.OriginalOrderId,
                BookReturnStatus.PaymentNotCompleted,
                "Chỉ đơn hàng đã thanh toán thành công mới được trả.");
        }

        if (!order.IsWithinReturnWindow)
        {
            return Failure(
                normalizedRequest.ReturnId,
                normalizedRequest.OriginalOrderId,
                BookReturnStatus.ReturnWindowExpired,
                "Đơn hàng đã quá thời hạn trả 7 ngày.");
        }

        foreach (var line in normalizedRequest.Lines)
        {
            var originalLine = order.Lines.SingleOrDefault(item =>
                item.OriginalOrderLineId == line.OriginalOrderLineId);

            if (originalLine is null)
            {
                return Failure(
                    normalizedRequest.ReturnId,
                    normalizedRequest.OriginalOrderId,
                    BookReturnStatus.OrderLineNotFound,
                    "Dòng sách không thuộc đơn hàng gốc.");
            }

            if (originalLine.OriginalUnitPrice is not > 0m)
            {
                return Failure(
                    normalizedRequest.ReturnId,
                    normalizedRequest.OriginalOrderId,
                    BookReturnStatus.OriginalPriceUnavailable,
                    $"Không thể trả sách {originalLine.BookId}: đơn giá gốc không hợp lệ.");
            }

            if (line.ReturnQuantity > originalLine.RemainingQuantity)
            {
                return Failure(
                    normalizedRequest.ReturnId,
                    normalizedRequest.OriginalOrderId,
                    BookReturnStatus.QuantityExceedsRemaining,
                    $"Số lượng trả sách {originalLine.BookId} vượt quá số lượng còn được trả.");
            }
        }

        return await repository.CreateReturnAsync(
            normalizedRequest,
            cancellationToken);
    }

    private static BookReturnResult Failure(
        string? returnId,
        string? orderId,
        BookReturnStatus status,
        string error)
    {
        return new BookReturnResult(
            status,
            returnId ?? string.Empty,
            orderId ?? string.Empty,
            null,
            null,
            Array.Empty<BookReturnLineResult>(),
            0m,
            error);
    }
}
