
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BookSales.Application.Sales;

public sealed class SaleProcessingService(
    ISaleRepository repository) : ISaleProcessingService
{
    public Task<IReadOnlyList<SaleBookOption>> GetBookOptionsAsync(
        CancellationToken cancellationToken)
    {
        return repository.GetBookOptionsAsync(cancellationToken);
    }

    // UC01: Xử lý bán hàng tiền mặt.
    public Task<ProcessSaleResult> ProcessAsync(
        ProcessSaleRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsValidRequest(request, out var error))
        {
            return Task.FromResult(
                Invalid(request.OrderId, error));
        }

        var normalizedRequest = NormalizeRequest(request);

        return repository.ProcessAsync(
            normalizedRequest,
            cancellationToken);
    }

    // UC02: Tạo đơn hàng QR Pending.
    public Task<ProcessSaleResult> CreateQrPendingAsync(
        ProcessSaleRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsValidRequest(request, out var error))
        {
            return Task.FromResult(
                Invalid(request.OrderId, error));
        }

        var normalizedRequest = NormalizeRequest(request);

        return repository.CreateQrPendingAsync(
            normalizedRequest,
            cancellationToken);
    }

    // UC02: Xác nhận thanh toán QR.
    public Task<ProcessSaleResult> ConfirmQrPaymentAsync(
        string orderId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(orderId) ||
            orderId.Length > 200)
        {
            return Task.FromResult(
                Invalid(
                    orderId,
                    "Mã đơn hàng không hợp lệ."));
        }

        return repository.ConfirmQrPaymentAsync(
            orderId.Trim(),
            cancellationToken);
    }

    // Kiểm tra dữ liệu đầu vào.
    private static bool IsValidRequest(
        ProcessSaleRequest request,
        out string error)
    {
        if (request is null)
        {
            error = "Thông tin đơn hàng không hợp lệ.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.OrderId) ||
            request.OrderId.Length > 200)
        {
            error = "Mã đơn hàng không hợp lệ.";
            return false;
        }

        if (!string.IsNullOrWhiteSpace(request.CustomerId) &&
            request.CustomerId.Length > 200)
        {
            error = "Mã khách hàng không hợp lệ.";
            return false;
        }

        if (request.Lines is null ||
            request.Lines.Count == 0)
        {
            error = "Đơn hàng phải có ít nhất một đầu sách.";
            return false;
        }

        foreach (var line in request.Lines)
        {
            if (line is null)
            {
                error = "Thông tin sản phẩm không hợp lệ.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(line.BookId) ||
                line.BookId.Length > 200)
            {
                error = "Mã sách không hợp lệ.";
                return false;
            }

            if (line.Quantity <= 0 ||
                line.Quantity != decimal.Truncate(line.Quantity))
            {
                error = "Số lượng sách phải là số nguyên lớn hơn 0.";
                return false;
            }
        }

        error = string.Empty;
        return true;
    }

    // Gộp các dòng sách trùng mã.
    private static ProcessSaleRequest NormalizeRequest(
        ProcessSaleRequest request)
    {
        var normalizedLines = request.Lines
            .GroupBy(
                line => line.BookId.Trim(),
                StringComparer.OrdinalIgnoreCase)
            .Select(group => new ProcessSaleLineRequest(
                group.Key,
                group.Sum(line => line.Quantity)))
            .ToList();

        return new ProcessSaleRequest(
            request.OrderId.Trim(),
            string.IsNullOrWhiteSpace(request.CustomerId)
                ? null
                : request.CustomerId.Trim(),
            normalizedLines);
    }

    // Tạo kết quả khi dữ liệu không hợp lệ.
    private static ProcessSaleResult Invalid(
        string? orderId,
        string error)
    {
        return new ProcessSaleResult(
            ProcessSaleStatus.InvalidRequest,
            orderId ?? string.Empty,
            null,
            Array.Empty<ProcessSaleLineResult>(),
            error,
            null,
            null);
    }
}
