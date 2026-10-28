using BookSalesManagement.Application.Abstractions;
using BookSalesManagement.Domain.Inventory;

namespace BookSalesManagement.Infrastructure.Persistence;

public sealed class EfInventoryAdjustmentStore : IInventoryAdjustmentStore
{
    private readonly BookSalesDbContext _db;

    public EfInventoryAdjustmentStore(BookSalesDbContext db)
    {
        _db = db;
    }

    public Task AddAsync(InventoryAdjustment adjustment, CancellationToken cancellationToken = default)
    {
        _db.InventoryAdjustments.Add(adjustment);
        return Task.CompletedTask;
    }

    public Task LinkCountAsync(string countId, string adjustmentId, CancellationToken cancellationToken = default)
    {
        _db.CountAdjustments.Add(new CountAdjustment(countId, adjustmentId));
        return Task.CompletedTask;
    }
}

public sealed class EfInventoryMovementStore : IInventoryMovementStore
{
    private readonly BookSalesDbContext _db;

    public EfInventoryMovementStore(BookSalesDbContext db)
    {
        _db = db;
    }

    public Task AddRangeAsync(IEnumerable<InventoryMovement> movements, CancellationToken cancellationToken = default)
    {
        _db.InventoryMovements.AddRange(movements);
        return Task.CompletedTask;
    }
}
