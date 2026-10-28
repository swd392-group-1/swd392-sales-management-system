using BookSalesManagement.Application.Dtos;
using BookSalesManagement.Domain.Inventory;

namespace BookSalesManagement.Application.Abstractions;

public interface IInventoryCountStore
{
    Task<InventoryCount?> FindAsync(string countId, CancellationToken cancellationToken = default);

    Task AddAsync(InventoryCount count, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InventoryCountSummary>> ListAsync(CancellationToken cancellationToken = default);

    Task<string?> FindAdjustmentIdAsync(string countId, CancellationToken cancellationToken = default);
}

public interface IStockStore
{
    Task EnsureStockRowsAsync(IReadOnlyCollection<string> bookIds, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<string, decimal>> GetQuantitiesAsync(
        IReadOnlyCollection<string> bookIds,
        CancellationToken cancellationToken = default);

    Task<bool> TrySetQuantityAsync(
        string bookId,
        decimal expectedQuantity,
        decimal newQuantity,
        CancellationToken cancellationToken = default);
}

public interface IInventoryAdjustmentStore
{
    Task AddAsync(InventoryAdjustment adjustment, CancellationToken cancellationToken = default);

    Task LinkCountAsync(string countId, string adjustmentId, CancellationToken cancellationToken = default);
}

public interface IInventoryMovementStore
{
    Task AddRangeAsync(IEnumerable<InventoryMovement> movements, CancellationToken cancellationToken = default);
}

public interface IInventoryAuditStore
{
    Task<InventoryAudit?> FindAsync(string auditId, CancellationToken cancellationToken = default);

    Task AddAsync(InventoryAudit audit, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InventoryAuditSummary>> ListAsync(CancellationToken cancellationToken = default);
}
