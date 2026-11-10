
namespace BookSales.Application.Sales;

public interface ISaleRepository
{
    Task<IReadOnlyList<SaleBookOption>> GetBookOptionsAsync(
        CancellationToken cancellationToken);

    Task<ProcessSaleResult> ProcessAsync(
        ProcessSaleRequest request,
        CancellationToken cancellationToken);

    Task<ProcessSaleResult> CreateQrPendingAsync(
        ProcessSaleRequest request,
        CancellationToken cancellationToken);

    Task<ProcessSaleResult> ConfirmQrPaymentAsync(
        string orderId,
        CancellationToken cancellationToken);
}
