using System.Data;
using System.Security.Cryptography;
using System.Text;
using BookSales.Application.Returns;
using BookSales.Domain.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace BookSales.Infrastructure.Persistence.Returns;

public sealed class EfBookReturnRepository(
    BookSalesDbContext db,
    TimeProvider timeProvider) : IBookReturnRepository
{
    private const decimal MaximumLineRefund = 999_999_999_999_999.9999m;

    public async Task<ReturnableOrderDetails?> GetOrderForReturnAsync(
        string orderId,
        CancellationToken cancellationToken)
    {
        var order = await db.SalesOrders
            .AsNoTracking()
            .Include(item => item.Lines)
                .ThenInclude(line => line.Book)
            .Include(item => item.OrderPayments)
                .ThenInclude(item => item.Payment)
            .SingleOrDefaultAsync(
                item => item.OrderId == orderId,
                cancellationToken);

        return order is null
            ? null
            : await ToReturnableOrderAsync(order, cancellationToken);
    }

    public async Task<IReadOnlyList<BookReturnSummary>> GetReturnHistoryAsync(
        string? orderId,
        CancellationToken cancellationToken)
    {
        var query = db.BookReturns.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(orderId))
        {
            query = query.Where(bookReturn =>
                bookReturn.OriginalOrderId == orderId);
        }

        return await query
            .OrderByDescending(bookReturn => bookReturn.ReturnedAtUtc)
            .Select(bookReturn => new BookReturnSummary(
                bookReturn.ReturnId,
                bookReturn.OriginalOrderId,
                bookReturn.ReturnedAtUtc,
                bookReturn.Lines.Sum(line => (decimal?)line.RefundAmount) ?? 0m))
            .ToListAsync(cancellationToken);
    }

    public Task<BookReturnResult?> GetReturnByIdAsync(
        string returnId,
        CancellationToken cancellationToken)
    {
        return LoadReturnAsync(
            returnId,
            BookReturnStatus.Completed,
            cancellationToken);
    }

    public async Task<BookReturnResult> CreateReturnAsync(
        BookReturnRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Failure(
                string.Empty,
                string.Empty,
                BookReturnStatus.InvalidRequest,
                "Thông tin trả sách không hợp lệ.");
        }

        if (!IsValidRequest(request))
        {
            return Failure(
                request.ReturnId,
                request.OriginalOrderId,
                BookReturnStatus.InvalidRequest,
                "Thông tin trả sách không hợp lệ.");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        try
        {
            var order = await db.SalesOrders
                .FromSqlInterpolated($"SELECT * FROM dbo.SalesOrder WITH (UPDLOCK, HOLDLOCK) WHERE order_id = {request.OriginalOrderId}")
                .Include(item => item.Lines)
                    .ThenInclude(line => line.Book)
                .Include(item => item.OrderPayments)
                    .ThenInclude(item => item.Payment)
                .SingleOrDefaultAsync(cancellationToken);

            if (order is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Failure(
                    request.ReturnId,
                    request.OriginalOrderId,
                    BookReturnStatus.OrderNotFound,
                    "Không tìm thấy đơn hàng gốc.");
            }

            var existing = await LoadReturnAsync(
                request.ReturnId,
                BookReturnStatus.AlreadyProcessed,
                cancellationToken);

            if (existing is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return IsSameRequest(existing, request)
                    ? existing
                    : Failure(
                        request.ReturnId,
                        request.OriginalOrderId,
                        BookReturnStatus.Conflict,
                        "Mã trả hàng đã được sử dụng cho nội dung khác.");
            }

            var payment = order.OrderPayments
                .Select(orderPayment => orderPayment.Payment)
                .FirstOrDefault(item => item.PaymentStatus == "Completed");

            if (payment is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Failure(
                    request.ReturnId,
                    request.OriginalOrderId,
                    BookReturnStatus.PaymentNotCompleted,
                    "Chỉ đơn hàng đã thanh toán thành công mới được trả.");
            }

            var todayUtc = DateOnly.FromDateTime(
                timeProvider.GetUtcNow().UtcDateTime);

            if (!ReturnPeriod.IsWithinWindow(order.OrderDate, todayUtc))
            {
                await transaction.RollbackAsync(cancellationToken);
                return Failure(
                    request.ReturnId,
                    request.OriginalOrderId,
                    BookReturnStatus.ReturnWindowExpired,
                    "Đơn hàng đã quá thời hạn trả 7 ngày.");
            }

            var originalLines = order.Lines.ToDictionary(
                line => line.OrderLineId);
            var requestedLineIds = request.Lines
                .Select(line => line.OriginalOrderLineId)
                .ToArray();
            var previousReturns = await db.BookReturnLines
                .AsNoTracking()
                .Where(line => requestedLineIds.Contains(line.OriginalOrderLineId))
                .GroupBy(line => line.OriginalOrderLineId)
                .Select(group => new
                {
                    OriginalOrderLineId = group.Key,
                    ReturnedQuantity = group.Sum(line => line.ReturnedQuantity)
                })
                .ToDictionaryAsync(
                    item => item.OriginalOrderLineId,
                    item => item.ReturnedQuantity,
                    cancellationToken);

            var returnLines = new List<BookReturnLineResult>(request.Lines.Count);
            decimal totalRefund = 0m;

            foreach (var requestedLine in request.Lines)
            {
                if (!originalLines.TryGetValue(
                    requestedLine.OriginalOrderLineId,
                    out var originalLine))
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return Failure(
                        request.ReturnId,
                        request.OriginalOrderId,
                        BookReturnStatus.OrderLineNotFound,
                        "Dòng sách không thuộc đơn hàng gốc.");
                }

                if (requestedLine.ReturnQuantity <= 0m ||
                    requestedLine.ReturnQuantity != decimal.Round(requestedLine.ReturnQuantity, 10) ||
                    !requestedLine.IsBookNewConfirmed)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return Failure(
                        request.ReturnId,
                        request.OriginalOrderId,
                        requestedLine.IsBookNewConfirmed
                            ? BookReturnStatus.InvalidRequest
                            : BookReturnStatus.BookNotNew,
                        requestedLine.IsBookNewConfirmed
                            ? "Số lượng trả phải dương và không vượt quá 10 chữ số thập phân."
                            : "Nhân viên bán hàng phải xác nhận sách còn mới.");
                }

                if (originalLine.UnitPrice is not > 0m)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return Failure(
                        request.ReturnId,
                        request.OriginalOrderId,
                        BookReturnStatus.OriginalPriceUnavailable,
                        $"Không thể trả sách {originalLine.BookId}: đơn giá gốc không hợp lệ.");
                }

                var alreadyReturned = previousReturns.GetValueOrDefault(
                    requestedLine.OriginalOrderLineId);
                var remainingQuantity = originalLine.Quantity - alreadyReturned;

                if (requestedLine.ReturnQuantity > remainingQuantity)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return Failure(
                        request.ReturnId,
                        request.OriginalOrderId,
                        BookReturnStatus.QuantityExceedsRemaining,
                        $"Số lượng trả sách {originalLine.BookId} vượt quá số lượng còn được trả.");
                }

                decimal refundAmount;
                try
                {
                    refundAmount = decimal.Round(
                        checked(requestedLine.ReturnQuantity * originalLine.UnitPrice.Value),
                        4,
                        MidpointRounding.AwayFromZero);
                    totalRefund = checked(totalRefund + refundAmount);
                }
                catch (OverflowException)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return Failure(
                        request.ReturnId,
                        request.OriginalOrderId,
                        BookReturnStatus.InvalidRequest,
                        "Số tiền hoàn vượt quá giới hạn lưu trữ.");
                }

                if (refundAmount > MaximumLineRefund)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return Failure(
                        request.ReturnId,
                        request.OriginalOrderId,
                        BookReturnStatus.InvalidRequest,
                        "Số tiền hoàn vượt quá giới hạn lưu trữ.");
                }

                returnLines.Add(new BookReturnLineResult(
                    originalLine.OrderLineId,
                    originalLine.BookId,
                    originalLine.Book?.Title,
                    requestedLine.ReturnQuantity,
                    originalLine.UnitPrice.Value,
                    refundAmount));
            }

            var bookIds = returnLines
                .Select(line => line.BookId)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var stockedBookIds = await db.BookStocks
                .AsNoTracking()
                .Where(stock => bookIds.Contains(stock.BookId))
                .Select(stock => stock.BookId)
                .ToListAsync(cancellationToken);

            if (bookIds.Any(bookId => !stockedBookIds.Contains(
                    bookId,
                    StringComparer.OrdinalIgnoreCase)))
            {
                await transaction.RollbackAsync(cancellationToken);
                return Failure(
                    request.ReturnId,
                    request.OriginalOrderId,
                    BookReturnStatus.StockUnavailable,
                    "Không tìm thấy tồn kho cho một hoặc nhiều đầu sách.");
            }

            var returnedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
            db.BookReturns.Add(new BookReturn
            {
                ReturnId = request.ReturnId,
                OriginalOrderId = request.OriginalOrderId,
                ReturnedAtUtc = returnedAtUtc,
                Lines = returnLines.Select(line => new BookReturnLine
                {
                    OriginalOrderLineId = line.OriginalOrderLineId,
                    ReturnedQuantity = line.ReturnedQuantity,
                    RefundAmount = line.RefundAmount
                }).ToList()
            });

            await db.SaveChangesAsync(cancellationToken);

            foreach (var line in returnLines)
            {
                var updatedRows = await db.BookStocks
                    .Where(stock => stock.BookId == line.BookId)
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(
                            stock => stock.CurrentQuantity,
                            stock => stock.CurrentQuantity + line.ReturnedQuantity),
                        cancellationToken);

                if (updatedRows != 1)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    db.ChangeTracker.Clear();
                    return Failure(
                        request.ReturnId,
                        request.OriginalOrderId,
                        BookReturnStatus.StockUnavailable,
                        $"Không thể cập nhật tồn kho sách {line.BookId}.");
                }
            }

            db.InventoryMovements.AddRange(
                returnLines.Select(line => new InventoryMovement
                {
                    PostingKey = CreatePostingKey(
                        request.ReturnId,
                        line.OriginalOrderLineId),
                    BookId = line.BookId,
                    QuantityDelta = line.ReturnedQuantity,
                    OccurredAtUtc = returnedAtUtc
                }));

            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new BookReturnResult(
                BookReturnStatus.Completed,
                request.ReturnId,
                request.OriginalOrderId,
                returnedAtUtc,
                payment.PaymentMethod,
                returnLines,
                totalRefund,
                null);
        }
        catch (DbUpdateException exception)
            when (IsUniqueViolation(exception))
        {
            await transaction.RollbackAsync(CancellationToken.None);
            db.ChangeTracker.Clear();

            var existing = await LoadReturnAsync(
                request.ReturnId,
                BookReturnStatus.AlreadyProcessed,
                cancellationToken);

            if (existing is not null && IsSameRequest(existing, request))
            {
                return existing;
            }

            if (existing is not null)
            {
                return Failure(
                    request.ReturnId,
                    request.OriginalOrderId,
                    BookReturnStatus.Conflict,
                    "Mã trả hàng đã được sử dụng cho nội dung khác.");
            }

            throw;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            db.ChangeTracker.Clear();
            throw;
        }
    }

    private async Task<ReturnableOrderDetails> ToReturnableOrderAsync(
        SalesOrder order,
        CancellationToken cancellationToken)
    {
        var lineIds = order.Lines
            .Select(line => line.OrderLineId)
            .ToArray();
        var returnedByLine = await db.BookReturnLines
            .AsNoTracking()
            .Where(line => lineIds.Contains(line.OriginalOrderLineId))
            .GroupBy(line => line.OriginalOrderLineId)
            .Select(group => new
            {
                OriginalOrderLineId = group.Key,
                ReturnedQuantity = group.Sum(line => line.ReturnedQuantity)
            })
            .ToDictionaryAsync(
                item => item.OriginalOrderLineId,
                item => item.ReturnedQuantity,
                cancellationToken);

        var payments = order.OrderPayments
            .Select(item => item.Payment)
            .ToArray();
        var completedPayment = payments.FirstOrDefault(
            item => item.PaymentStatus == "Completed");
        var payment = completedPayment ?? payments.FirstOrDefault();
        var todayUtc = DateOnly.FromDateTime(
            timeProvider.GetUtcNow().UtcDateTime);
        var withinWindow = ReturnPeriod.IsWithinWindow(order.OrderDate, todayUtc);

        var lines = order.Lines
            .Select(line =>
            {
                var previousQuantity = returnedByLine.GetValueOrDefault(
                    line.OrderLineId);
                var remainingQuantity = Math.Max(
                    0m,
                    line.Quantity - previousQuantity);

                return new ReturnableOrderLine(
                    line.OrderLineId,
                    line.BookId,
                    line.Book?.Title,
                    line.Quantity,
                    previousQuantity,
                    remainingQuantity,
                    line.UnitPrice);
            })
            .ToArray();

        var isPaid = completedPayment is not null;
        string? message = null;

        if (!isPaid)
        {
            message = "Đơn hàng chưa được thanh toán thành công.";
        }
        else if (order.OrderDate is null)
        {
            message = "Không xác định được ngày bán nên không thể kiểm tra thời hạn trả.";
        }
        else if (!withinWindow)
        {
            message = "Đơn hàng đã quá thời hạn trả 7 ngày.";
        }
        else if (!lines.Any(line => line.RemainingQuantity > 0m))
        {
            message = "Tất cả sách trong đơn hàng đã được trả đủ.";
        }
        else if (!lines.Any(line =>
                     line.RemainingQuantity > 0m &&
                     line.OriginalUnitPrice is > 0m))
        {
            message = "Không có dòng sách nào có đơn giá gốc hợp lệ để trả.";
        }

        return new ReturnableOrderDetails(
            order.OrderId,
            order.OrderDate,
            order.TotalOrderAmount,
            payment?.PaymentMethod,
            payment?.PaymentStatus,
            isPaid,
            withinWindow,
            message,
            lines);
    }

    private async Task<BookReturnResult?> LoadReturnAsync(
        string returnId,
        BookReturnStatus status,
        CancellationToken cancellationToken)
    {
        var bookReturn = await db.BookReturns
            .AsNoTracking()
            .Include(item => item.OriginalOrder)
                .ThenInclude(order => order.OrderPayments)
                    .ThenInclude(orderPayment => orderPayment.Payment)
            .Include(item => item.Lines)
                .ThenInclude(line => line.OriginalOrderLine)
                    .ThenInclude(orderLine => orderLine.Book)
            .SingleOrDefaultAsync(
                item => item.ReturnId == returnId,
                cancellationToken);

        if (bookReturn is null)
        {
            return null;
        }

        var originalPayment = bookReturn.OriginalOrder.OrderPayments
            .Select(item => item.Payment)
            .FirstOrDefault(item => item.PaymentStatus == "Completed");
        var lines = bookReturn.Lines
            .Select(line => new BookReturnLineResult(
                line.OriginalOrderLineId,
                line.OriginalOrderLine.BookId,
                line.OriginalOrderLine.Book?.Title,
                line.ReturnedQuantity,
                line.OriginalOrderLine.UnitPrice ?? 0m,
                line.RefundAmount))
            .ToArray();

        return new BookReturnResult(
            status,
            bookReturn.ReturnId,
            bookReturn.OriginalOrderId,
            bookReturn.ReturnedAtUtc,
            originalPayment?.PaymentMethod,
            lines,
            lines.Sum(line => line.RefundAmount),
            null);
    }

    private static bool IsValidRequest(BookReturnRequest request)
    {
        return request is not null &&
            !string.IsNullOrWhiteSpace(request.ReturnId) &&
            request.ReturnId.Length <= 200 &&
            !string.IsNullOrWhiteSpace(request.OriginalOrderId) &&
            request.OriginalOrderId.Length <= 200 &&
            request.Lines is { Count: > 0 } &&
            request.IsCashRefundConfirmed &&
            request.Lines.All(line =>
                line is not null &&
                line.OriginalOrderLineId > 0 &&
                line.ReturnQuantity > 0m &&
                line.ReturnQuantity == decimal.Round(line.ReturnQuantity, 10) &&
                line.IsBookNewConfirmed) &&
            request.Lines.Select(line => line.OriginalOrderLineId)
                .Distinct()
                .Count() == request.Lines.Count;
    }

    private static bool IsSameRequest(
        BookReturnResult existing,
        BookReturnRequest request)
    {
        return string.Equals(
                existing.OriginalOrderId,
                request.OriginalOrderId,
                StringComparison.OrdinalIgnoreCase) &&
            existing.Lines.Count == request.Lines.Count &&
            existing.Lines.All(existingLine =>
                request.Lines.Any(requestedLine =>
                    requestedLine.OriginalOrderLineId == existingLine.OriginalOrderLineId &&
                    requestedLine.ReturnQuantity == existingLine.ReturnedQuantity));
    }

    private static string CreatePostingKey(
        string returnId,
        long originalOrderLineId)
    {
        var source = Encoding.UTF8.GetBytes(
            $"RETURN:{returnId}:{originalOrderLineId}");
        var digest = Convert.ToHexString(SHA256.HashData(source));
        return $"RETURN:{digest}";
    }

    private static bool IsUniqueViolation(DbUpdateException exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is SqlException sqlException &&
                sqlException.Errors
                    .Cast<SqlError>()
                    .Any(error => error.Number is 2601 or 2627))
            {
                return true;
            }
        }

        return false;
    }

    private static BookReturnResult Failure(
        string returnId,
        string orderId,
        BookReturnStatus status,
        string error)
    {
        return new BookReturnResult(
            status,
            returnId,
            orderId,
            null,
            null,
            Array.Empty<BookReturnLineResult>(),
            0m,
            error);
    }
}
