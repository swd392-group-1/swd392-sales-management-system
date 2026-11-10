
using System.Data;
using System.Security.Cryptography;
using System.Text;
using BookSales.Application.Sales;
using BookSales.Domain.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace BookSales.Infrastructure.Persistence.Sales;

public sealed class EfSaleRepository(
    BookSalesDbContext db,
    TimeProvider timeProvider) : ISaleRepository
{
    private const decimal MaximumOrderAmount = 999_999_999_999_999.9999m;

    public async Task<IReadOnlyList<SaleBookOption>> GetBookOptionsAsync(
        CancellationToken cancellationToken)
    {
        return await db.Books
            .AsNoTracking()
            .OrderBy(book => book.Title)
            .Select(book => new SaleBookOption(
                book.BookId,
                book.Title,
                book.CurrentPrice,
                book.Stock == null ? 0m : book.Stock.CurrentQuantity))
            .ToListAsync(cancellationToken);
    }

    // UC01: Bán hàng bằng tiền mặt.
    public async Task<ProcessSaleResult> ProcessAsync(
        ProcessSaleRequest request,
        CancellationToken cancellationToken)
    {
        await using var transaction =
            await db.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                cancellationToken);

        var existing = await LoadOrderAsync(
            request.OrderId,
            cancellationToken);

        if (existing is not null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return ExistingOrderResult(existing, request);
        }

        if (request.CustomerId is not null &&
            !await db.Customers.AnyAsync(
                customer => customer.CustomerId == request.CustomerId,
                cancellationToken))
        {
            await transaction.RollbackAsync(cancellationToken);

            return Failure(
                request.OrderId,
                ProcessSaleStatus.CustomerNotFound,
                "Không tìm thấy khách hàng đã chọn.");
        }

        var bookIds = request.Lines
            .Select(line => line.BookId)
            .ToArray();

        var books = await db.Books
            .AsNoTracking()
            .Where(book => bookIds.Contains(book.BookId))
            .Select(book => new
            {
                book.BookId,
                book.Title,
                book.CurrentPrice,
                CurrentQuantity = book.Stock == null
                    ? (decimal?)null
                    : book.Stock.CurrentQuantity
            })
            .ToListAsync(cancellationToken);

        var booksById = books.ToDictionary(
            book => book.BookId,
            StringComparer.OrdinalIgnoreCase);

        var pricedLines = new List<ProcessSaleLineResult>(
            request.Lines.Count);

        decimal totalAmount = 0m;

        foreach (var requestedLine in request.Lines)
        {
            if (!booksById.TryGetValue(
                requestedLine.BookId,
                out var book))
            {
                await transaction.RollbackAsync(cancellationToken);

                return Failure(
                    request.OrderId,
                    ProcessSaleStatus.InvalidRequest,
                    $"Không tìm thấy sách {requestedLine.BookId}.");
            }

            if (book.CurrentPrice is not > 0m)
            {
                await transaction.RollbackAsync(cancellationToken);

                return Failure(
                    request.OrderId,
                    ProcessSaleStatus.InvalidRequest,
                    $"Sách {requestedLine.BookId} chưa có giá bán hợp lệ.");
            }

            if (book.CurrentQuantity is null ||
                book.CurrentQuantity < requestedLine.Quantity)
            {
                await transaction.RollbackAsync(cancellationToken);

                return Failure(
                    request.OrderId,
                    ProcessSaleStatus.InsufficientStock,
                    $"Sách {requestedLine.BookId} không đủ tồn kho.");
            }

            decimal lineTotal;

            try
            {
                lineTotal = decimal.Round(
                    book.CurrentPrice.Value * requestedLine.Quantity,
                    4,
                    MidpointRounding.AwayFromZero);

                totalAmount = checked(totalAmount + lineTotal);
            }
            catch (OverflowException)
            {
                await transaction.RollbackAsync(cancellationToken);

                return Failure(
                    request.OrderId,
                    ProcessSaleStatus.InvalidRequest,
                    "Tổng tiền vượt quá giới hạn lưu trữ.");
            }

            if (totalAmount > MaximumOrderAmount)
            {
                await transaction.RollbackAsync(cancellationToken);

                return Failure(
                    request.OrderId,
                    ProcessSaleStatus.InvalidRequest,
                    "Tổng tiền vượt quá giới hạn lưu trữ.");
            }

            pricedLines.Add(new ProcessSaleLineResult(
                book.BookId,
                book.Title,
                requestedLine.Quantity,
                book.CurrentPrice.Value,
                lineTotal));
        }

        var order = new SalesOrder
        {
            OrderId = request.OrderId,
            CustomerId = request.CustomerId,
            OrderDate = DateOnly.FromDateTime(
                timeProvider.GetUtcNow().UtcDateTime),
            TotalOrderAmount = totalAmount
        };

        db.SalesOrders.Add(order);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (IsUniqueViolation(exception))
        {
            await transaction.RollbackAsync(cancellationToken);
            db.ChangeTracker.Clear();

            var concurrentOrder = await LoadOrderAsync(
                request.OrderId,
                cancellationToken);

            if (concurrentOrder is not null)
            {
                return ExistingOrderResult(
                    concurrentOrder,
                    request);
            }

            throw;
        }
        catch (DbUpdateException exception)
            when (IsForeignKeyViolation(exception))
        {
            await transaction.RollbackAsync(cancellationToken);
            db.ChangeTracker.Clear();

            return Failure(
                request.OrderId,
                ProcessSaleStatus.CustomerNotFound,
                "Khách hàng không còn tồn tại. Vui lòng chọn lại khách hàng.");
        }

        // Trừ kho khi thanh toán tiền mặt.
        foreach (var line in pricedLines)
        {
            var affectedRows = await db.BookStocks
                .Where(stock =>
                    stock.BookId == line.BookId &&
                    stock.CurrentQuantity >= line.Quantity)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(
                        stock => stock.CurrentQuantity,
                        stock => stock.CurrentQuantity - line.Quantity),
                    cancellationToken);

            if (affectedRows != 1)
            {
                await transaction.RollbackAsync(cancellationToken);
                db.ChangeTracker.Clear();

                return Failure(
                    request.OrderId,
                    ProcessSaleStatus.InsufficientStock,
                    $"Sách {line.BookId} không đủ tồn kho.");
            }
        }

        var paymentId = Guid.NewGuid().ToString("N");

        var payment = new Payment
        {
            PaymentId = paymentId,
            PaymentMethod = "Cash",
            PaymentStatus = "Completed"
        };

        db.SalesOrderLines.AddRange(
            pricedLines.Select(line => new SalesOrderLine
            {
                OrderId = request.OrderId,
                BookId = line.BookId,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice
            }));

        db.Payments.Add(payment);

        db.OrderPayments.Add(new OrderPayment
        {
            OrderId = request.OrderId,
            PaymentId = paymentId
        });

        db.InventoryMovements.AddRange(
            pricedLines.Select(line => new InventoryMovement
            {
                PostingKey = CreatePostingKey(
                    request.OrderId,
                    line.BookId),

                BookId = line.BookId,
                QuantityDelta = -line.Quantity,
                OccurredAtUtc = timeProvider.GetUtcNow().UtcDateTime
            }));

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new ProcessSaleResult(
            ProcessSaleStatus.Completed,
            request.OrderId,
            totalAmount,
            pricedLines,
            null,
            payment.PaymentMethod,
            payment.PaymentStatus);
    }

    // UC02: Tạo đơn hàng QR đang chờ thanh toán.
    public async Task<ProcessSaleResult> CreateQrPendingAsync(
        ProcessSaleRequest request,
        CancellationToken cancellationToken)
    {
        await using var transaction =
            await db.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                cancellationToken);

        var existing = await LoadOrderAsync(
            request.OrderId,
            cancellationToken);

        if (existing is not null)
        {
            await transaction.RollbackAsync(cancellationToken);

            return ExistingQrPaymentResult(existing, request);
        }

        if (request.CustomerId is not null &&
            !await db.Customers.AnyAsync(
                customer => customer.CustomerId == request.CustomerId,
                cancellationToken))
        {
            await transaction.RollbackAsync(cancellationToken);

            return Failure(
                request.OrderId,
                ProcessSaleStatus.CustomerNotFound,
                "Không tìm thấy khách hàng đã chọn.");
        }

        var bookIds = request.Lines
            .Select(line => line.BookId)
            .ToArray();

        var books = await db.Books
            .AsNoTracking()
            .Where(book => bookIds.Contains(book.BookId))
            .Select(book => new
            {
                book.BookId,
                book.Title,
                book.CurrentPrice,
                CurrentQuantity = book.Stock == null
                    ? (decimal?)null
                    : book.Stock.CurrentQuantity
            })
            .ToListAsync(cancellationToken);

        var booksById = books.ToDictionary(
            book => book.BookId,
            StringComparer.OrdinalIgnoreCase);

        var pricedLines = new List<ProcessSaleLineResult>(
            request.Lines.Count);

        decimal totalAmount = 0m;

        foreach (var requestedLine in request.Lines)
        {
            if (!booksById.TryGetValue(
                requestedLine.BookId,
                out var book))
            {
                await transaction.RollbackAsync(cancellationToken);

                return Failure(
                    request.OrderId,
                    ProcessSaleStatus.InvalidRequest,
                    $"Không tìm thấy sách {requestedLine.BookId}.");
            }

            if (book.CurrentPrice is not > 0m)
            {
                await transaction.RollbackAsync(cancellationToken);

                return Failure(
                    request.OrderId,
                    ProcessSaleStatus.InvalidRequest,
                    $"Sách {requestedLine.BookId} chưa có giá bán hợp lệ.");
            }

            if (book.CurrentQuantity is null ||
                book.CurrentQuantity < requestedLine.Quantity)
            {
                await transaction.RollbackAsync(cancellationToken);

                return Failure(
                    request.OrderId,
                    ProcessSaleStatus.InsufficientStock,
                    $"Sách {requestedLine.BookId} không đủ tồn kho.");
            }

            decimal lineTotal;

            try
            {
                lineTotal = decimal.Round(
                    book.CurrentPrice.Value * requestedLine.Quantity,
                    4,
                    MidpointRounding.AwayFromZero);

                totalAmount = checked(totalAmount + lineTotal);
            }
            catch (OverflowException)
            {
                await transaction.RollbackAsync(cancellationToken);

                return Failure(
                    request.OrderId,
                    ProcessSaleStatus.InvalidRequest,
                    "Tổng tiền vượt quá giới hạn lưu trữ.");
            }

            if (totalAmount > MaximumOrderAmount)
            {
                await transaction.RollbackAsync(cancellationToken);

                return Failure(
                    request.OrderId,
                    ProcessSaleStatus.InvalidRequest,
                    "Tổng tiền vượt quá giới hạn lưu trữ.");
            }

            pricedLines.Add(new ProcessSaleLineResult(
                book.BookId,
                book.Title,
                requestedLine.Quantity,
                book.CurrentPrice.Value,
                lineTotal));
        }

        var paymentId = Guid.NewGuid().ToString("N");

        db.SalesOrders.Add(new SalesOrder
        {
            OrderId = request.OrderId,
            CustomerId = request.CustomerId,
            OrderDate = DateOnly.FromDateTime(
                timeProvider.GetUtcNow().UtcDateTime),
            TotalOrderAmount = totalAmount
        });

        db.SalesOrderLines.AddRange(
            pricedLines.Select(line => new SalesOrderLine
            {
                OrderId = request.OrderId,
                BookId = line.BookId,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice
            }));

        db.Payments.Add(new Payment
        {
            PaymentId = paymentId,
            PaymentMethod = "QR",
            PaymentStatus = "Pending"
        });

        db.OrderPayments.Add(new OrderPayment
        {
            OrderId = request.OrderId,
            PaymentId = paymentId
        });

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (IsUniqueViolation(exception))
        {
            await transaction.RollbackAsync(cancellationToken);
            db.ChangeTracker.Clear();

            var concurrentOrder = await LoadOrderAsync(
                request.OrderId,
                cancellationToken);

            if (concurrentOrder is not null)
            {
                return ExistingQrPaymentResult(
                    concurrentOrder,
                    request);
            }

            throw;
        }
        catch (DbUpdateException exception)
            when (IsForeignKeyViolation(exception))
        {
            await transaction.RollbackAsync(cancellationToken);
            db.ChangeTracker.Clear();

            return Failure(
                request.OrderId,
                request.CustomerId is null
                    ? ProcessSaleStatus.InvalidRequest
                    : ProcessSaleStatus.CustomerNotFound,
                "Sách hoặc khách hàng không còn tồn tại. Vui lòng tải lại dữ liệu và thử lại.");
        }

        // Không trừ kho khi đơn QR còn Pending.
        return new ProcessSaleResult(
            ProcessSaleStatus.PaymentPending,
            request.OrderId,
            totalAmount,
            pricedLines,
            null,
            "QR",
            "Pending");
    }

    // UC02: Xác nhận thanh toán QR mô phỏng.
    public async Task<ProcessSaleResult> ConfirmQrPaymentAsync(
        string orderId,
        CancellationToken cancellationToken)
    {
        await using var transaction =
            await db.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                cancellationToken);

        var order = await db.SalesOrders
            .Include(item => item.Lines)
                .ThenInclude(line => line.Book)
            .Include(item => item.OrderPayments)
                .ThenInclude(item => item.Payment)
            .SingleOrDefaultAsync(
                item => item.OrderId == orderId,
                cancellationToken);

        if (order is null)
        {
            await transaction.RollbackAsync(cancellationToken);

            return Failure(
                orderId,
                ProcessSaleStatus.InvalidRequest,
                "Không tìm thấy đơn hàng.");
        }

        var payment = order.OrderPayments.Count == 1
            ? order.OrderPayments.Single().Payment
            : null;

        if (payment is null || payment.PaymentMethod != "QR")
        {
            await transaction.RollbackAsync(cancellationToken);

            return Failure(
                orderId,
                ProcessSaleStatus.OrderConflict,
                "Đơn hàng không sử dụng phương thức thanh toán QR.");
        }

        var resultLines = order.Lines
            .Select(line => new ProcessSaleLineResult(
                line.BookId,
                line.Book?.Title,
                line.Quantity,
                line.UnitPrice ?? 0m,
                decimal.Round(
                    (line.UnitPrice ?? 0m) * line.Quantity,
                    4,
                    MidpointRounding.AwayFromZero)))
            .ToArray();

        // Đơn đã hoàn tất: không trừ kho lần thứ hai.
        if (payment.PaymentStatus == "Completed")
        {
            await transaction.RollbackAsync(cancellationToken);

            return new ProcessSaleResult(
                ProcessSaleStatus.AlreadyProcessed,
                order.OrderId,
                order.TotalOrderAmount,
                resultLines,
                null,
                "QR",
                "Completed");
        }

        if (payment.PaymentStatus != "Pending")
        {
            await transaction.RollbackAsync(cancellationToken);

            return Failure(
                orderId,
                ProcessSaleStatus.OrderConflict,
                "Đơn hàng không ở trạng thái chờ thanh toán.");
        }

        if (order.TotalOrderAmount is null ||
            order.Lines.Count == 0 ||
            order.Lines.Any(line =>
                line.Quantity <= 0 ||
                line.UnitPrice is null or <= 0m))
        {
            await transaction.RollbackAsync(cancellationToken);

            return Failure(
                orderId,
                ProcessSaleStatus.InvalidRequest,
                "Chi tiết đơn hàng không hợp lệ.");
        }

        // Chỉ một yêu cầu được phép chuyển Pending sang Completed.
        var updatedPayments = await db.Payments
            .Where(item =>
                item.PaymentId == payment.PaymentId &&
                item.PaymentMethod == "QR" &&
                item.PaymentStatus == "Pending")
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    item => item.PaymentStatus,
                    "Completed"),
                cancellationToken);

        if (updatedPayments != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            db.ChangeTracker.Clear();

            return Failure(
                orderId,
                ProcessSaleStatus.OrderConflict,
                "Thanh toán đã được xử lý hoặc trạng thái đã thay đổi.");
        }

        // Trừ kho trong cùng transaction.
        foreach (var line in order.Lines)
        {
            var affectedRows = await db.BookStocks
                .Where(stock =>
                    stock.BookId == line.BookId &&
                    stock.CurrentQuantity >= line.Quantity)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(
                        stock => stock.CurrentQuantity,
                        stock => stock.CurrentQuantity - line.Quantity),
                    cancellationToken);

            if (affectedRows != 1)
            {
                await transaction.RollbackAsync(cancellationToken);
                db.ChangeTracker.Clear();

                return Failure(
                    orderId,
                    ProcessSaleStatus.InsufficientStock,
                    $"Sách {line.BookId} không đủ tồn kho để xác nhận thanh toán.");
            }
        }

        // Ghi lịch sử trừ kho.
        db.InventoryMovements.AddRange(
            order.Lines.Select(line => new InventoryMovement
            {
                PostingKey = CreatePostingKey(
                    orderId,
                    line.BookId),

                BookId = line.BookId,
                QuantityDelta = -line.Quantity,
                OccurredAtUtc = timeProvider.GetUtcNow().UtcDateTime
            }));

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            db.ChangeTracker.Clear();
            throw;
        }

        return new ProcessSaleResult(
            ProcessSaleStatus.Completed,
            orderId,
            order.TotalOrderAmount,
            resultLines,
            null,
            "QR",
            "Completed");
    }

    private async Task<SalesOrder?> LoadOrderAsync(
        string orderId,
        CancellationToken cancellationToken)
    {
        return await db.SalesOrders
            .AsNoTracking()
            .Include(order => order.Lines)
                .ThenInclude(line => line.Book)
            .Include(order => order.OrderPayments)
                .ThenInclude(orderPayment => orderPayment.Payment)
            .SingleOrDefaultAsync(
                order => order.OrderId == orderId,
                cancellationToken);
    }

    private static ProcessSaleResult ExistingOrderResult(
        SalesOrder order,
        ProcessSaleRequest request)
    {
        var storedGroups = order.Lines
            .GroupBy(
                line => line.BookId,
                StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var sameLines = storedGroups.Length == request.Lines.Count &&
            storedGroups.All(group =>
                group.Count() == 1 &&
                request.Lines.Any(requested =>
                    string.Equals(
                        group.Key,
                        requested.BookId,
                        StringComparison.OrdinalIgnoreCase) &&
                    group.Single().Quantity == requested.Quantity &&
                    group.Single().UnitPrice is > 0m));

        var sameCustomer = string.Equals(
            order.CustomerId,
            request.CustomerId,
            StringComparison.OrdinalIgnoreCase);

        var payment = order.OrderPayments
            .Select(orderPayment => orderPayment.Payment)
            .FirstOrDefault();

        var samePayment = order.OrderPayments.Count == 1 &&
            payment?.PaymentMethod == "Cash" &&
            payment.PaymentStatus == "Completed";

        if (!sameLines ||
            !sameCustomer ||
            !samePayment ||
            order.TotalOrderAmount is null)
        {
            return Failure(
                request.OrderId,
                ProcessSaleStatus.OrderConflict,
                "Mã đơn hàng đã được sử dụng cho nội dung khác.");
        }

        var lines = order.Lines
            .Select(line => new ProcessSaleLineResult(
                line.BookId,
                line.Book?.Title,
                line.Quantity,
                line.UnitPrice!.Value,
                decimal.Round(
                    line.UnitPrice.Value * line.Quantity,
                    4,
                    MidpointRounding.AwayFromZero)))
            .ToArray();

        return new ProcessSaleResult(
            ProcessSaleStatus.AlreadyProcessed,
            order.OrderId,
            order.TotalOrderAmount,
            lines,
            null,
            payment?.PaymentMethod,
            payment?.PaymentStatus);
    }

    private static ProcessSaleResult ExistingQrPaymentResult(
        SalesOrder order,
        ProcessSaleRequest request)
    {
        var storedGroups = order.Lines
            .GroupBy(
                line => line.BookId,
                StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var sameLines = storedGroups.Length == request.Lines.Count &&
            storedGroups.All(group =>
                group.Count() == 1 &&
                request.Lines.Any(requested =>
                    string.Equals(
                        group.Key,
                        requested.BookId,
                        StringComparison.OrdinalIgnoreCase) &&
                    group.Single().Quantity == requested.Quantity &&
                    group.Single().UnitPrice is > 0m));

        var sameCustomer = string.Equals(
            order.CustomerId,
            request.CustomerId,
            StringComparison.OrdinalIgnoreCase);

        var payment = order.OrderPayments
            .Select(orderPayment => orderPayment.Payment)
            .FirstOrDefault();

        var samePendingPayment = order.OrderPayments.Count == 1 &&
            payment?.PaymentMethod == "QR" &&
            payment.PaymentStatus == "Pending";

        if (!sameLines ||
            !sameCustomer ||
            !samePendingPayment ||
            order.TotalOrderAmount is null)
        {
            return Failure(
                request.OrderId,
                ProcessSaleStatus.OrderConflict,
                "Mã đơn hàng đã được sử dụng cho nội dung khác.");
        }

        var lines = order.Lines
            .Select(line => new ProcessSaleLineResult(
                line.BookId,
                line.Book?.Title,
                line.Quantity,
                line.UnitPrice!.Value,
                decimal.Round(
                    line.UnitPrice.Value * line.Quantity,
                    4,
                    MidpointRounding.AwayFromZero)))
            .ToArray();

        return new ProcessSaleResult(
            ProcessSaleStatus.PaymentPending,
            order.OrderId,
            order.TotalOrderAmount,
            lines,
            null,
            payment?.PaymentMethod,
            payment?.PaymentStatus);
    }

    private static ProcessSaleResult Failure(
        string orderId,
        ProcessSaleStatus status,
        string message)
    {
        return new ProcessSaleResult(
            status,
            orderId,
            null,
            Array.Empty<ProcessSaleLineResult>(),
            message,
            null,
            null);
    }

    private static string CreatePostingKey(
        string orderId,
        string bookId)
    {
        var input = Encoding.UTF8.GetBytes(
            $"{orderId}\0{bookId}");

        return "SALE:" +
            Convert.ToHexString(SHA256.HashData(input));
    }

    private static bool IsUniqueViolation(
        DbUpdateException exception)
    {
        for (
            Exception? current = exception;
            current is not null;
            current = current.InnerException)
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

    private static bool IsForeignKeyViolation(
        DbUpdateException exception)
    {
        for (
            Exception? current = exception;
            current is not null;
            current = current.InnerException)
        {
            if (current is SqlException sqlException &&
                sqlException.Errors
                    .Cast<SqlError>()
                    .Any(error => error.Number == 547))
            {
                return true;
            }
        }

        return false;
    }
}
