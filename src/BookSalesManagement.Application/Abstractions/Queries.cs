using BookSalesManagement.Application.Dtos;

namespace BookSalesManagement.Application.Abstractions;

public interface IInventoryQueryService
{
    Task<IReadOnlyList<StockOverviewRow>> GetStockOverviewAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdjustmentSummary>> GetAdjustmentsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MovementRow>> GetRecentMovementsAsync(int take, CancellationToken cancellationToken = default);
}

public interface IPurchaseQueryService
{
    Task<IReadOnlyList<PurchaseOrderRow>> GetPurchaseOrdersAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GoodsReceiptRow>> GetGoodsReceiptsAsync(CancellationToken cancellationToken = default);
}
