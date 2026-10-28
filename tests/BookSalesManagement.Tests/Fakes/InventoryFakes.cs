using System.Data;
using BookSalesManagement.Application.Abstractions;
using BookSalesManagement.Application.Common;
using BookSalesManagement.Application.Dtos;
using BookSalesManagement.Domain.Inventory;

namespace BookSalesManagement.Tests.Fakes;

public sealed class InMemoryCountStore : IInventoryCountStore
{
    private readonly Dictionary<string, InventoryCount> _counts = new(StringComparer.Ordinal);

    public IReadOnlyCollection<InventoryCount> All => _counts.Values;

    public Task<InventoryCount?> FindAsync(string countId, CancellationToken cancellationToken = default)
        => Task.FromResult(_counts.TryGetValue(countId, out var count) ? count : null);

    public Task AddAsync(InventoryCount count, CancellationToken cancellationToken = default)
    {
        _counts[count.Id] = count;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<InventoryCountSummary>> ListAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<InventoryCountSummary>>(Array.Empty<InventoryCountSummary>());

    public Task<string?> FindAdjustmentIdAsync(string countId, CancellationToken cancellationToken = default)
        => Task.FromResult<string?>(null);
}

public sealed class InMemoryStockStore : IStockStore
{
    public Dictionary<string, decimal> Quantities { get; } = new(StringComparer.Ordinal);

    public int TrySetCalls { get; private set; }

    public Action? BeforeTrySet { get; set; }

    public Task EnsureStockRowsAsync(IReadOnlyCollection<string> bookIds, CancellationToken cancellationToken = default)
    {
        foreach (var bookId in bookIds)
        {
            Quantities.TryAdd(bookId, 0);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyDictionary<string, decimal>> GetQuantitiesAsync(
        IReadOnlyCollection<string> bookIds,
        CancellationToken cancellationToken = default)
    {
        var result = bookIds
            .Where(Quantities.ContainsKey)
            .ToDictionary(bookId => bookId, bookId => Quantities[bookId], StringComparer.Ordinal);
        return Task.FromResult<IReadOnlyDictionary<string, decimal>>(result);
    }

    public Task<bool> TrySetQuantityAsync(
        string bookId,
        decimal expectedQuantity,
        decimal newQuantity,
        CancellationToken cancellationToken = default)
    {
        TrySetCalls++;
        BeforeTrySet?.Invoke();

        if (!Quantities.TryGetValue(bookId, out var current) || current != expectedQuantity)
        {
            return Task.FromResult(false);
        }

        Quantities[bookId] = newQuantity;
        return Task.FromResult(true);
    }
}

public sealed class InMemoryAdjustmentStore : IInventoryAdjustmentStore
{
    private readonly Dictionary<string, InventoryAdjustment> _adjustments = new(StringComparer.Ordinal);

    public IReadOnlyList<InventoryAdjustment> All => _adjustments.Values.ToArray();

    public Task AddAsync(InventoryAdjustment adjustment, CancellationToken cancellationToken = default)
    {
        _adjustments[adjustment.Id] = adjustment;
        return Task.CompletedTask;
    }

    public Task LinkCountAsync(string countId, string adjustmentId, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

public sealed class InMemoryMovementStore : IInventoryMovementStore
{
    private readonly List<InventoryMovement> _movements = new();

    public IReadOnlyList<InventoryMovement> All => _movements;

    public Task AddRangeAsync(IEnumerable<InventoryMovement> movements, CancellationToken cancellationToken = default)
    {
        _movements.AddRange(movements);
        return Task.CompletedTask;
    }
}

public sealed class InMemoryAuditStore : IInventoryAuditStore
{
    private readonly Dictionary<string, InventoryAudit> _audits = new(StringComparer.Ordinal);

    public IReadOnlyList<InventoryAudit> All => _audits.Values.ToArray();

    public Task<InventoryAudit?> FindAsync(string auditId, CancellationToken cancellationToken = default)
        => Task.FromResult(_audits.TryGetValue(auditId, out var audit) ? audit : null);

    public Task AddAsync(InventoryAudit audit, CancellationToken cancellationToken = default)
    {
        _audits[audit.Id] = audit;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<InventoryAuditSummary>> ListAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<InventoryAuditSummary>>(Array.Empty<InventoryAuditSummary>());
}

public sealed class InMemoryBookDirectory : IBookDirectory
{
    public HashSet<string> Existing { get; } = new(StringComparer.Ordinal);

    public Task<IReadOnlyList<BookOption>> GetOptionsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<BookOption>>(Array.Empty<BookOption>());

    public Task<IReadOnlyDictionary<string, string>> GetTitlesAsync(
        IEnumerable<string> bookIds,
        CancellationToken cancellationToken = default)
    {
        var titles = bookIds.ToDictionary(bookId => bookId, bookId => bookId, StringComparer.Ordinal);
        return Task.FromResult<IReadOnlyDictionary<string, string>>(titles);
    }

    public Task<bool> AllExistAsync(IEnumerable<string> bookIds, CancellationToken cancellationToken = default)
        => Task.FromResult(bookIds.All(Existing.Contains));
}

public sealed class InMemoryEmployeeDirectory : IEmployeeDirectory
{
    public Task<IReadOnlyDictionary<string, string>> GetNamesAsync(
        IEnumerable<string> employeeIds,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyDictionary<string, string>>(
            new Dictionary<string, string>(StringComparer.Ordinal));
}

public sealed class FixedClock : IClock
{
    public FixedClock(DateTime utcNow)
    {
        UtcNow = utcNow;
    }

    public DateTime UtcNow { get; }
}

public sealed class TestCurrentUser : ICurrentUser
{
    public bool IsAuthenticated => true;

    public string? AccountId => "ACC-TEST";

    public string? EmployeeId => "EMP-TEST";

    public string DisplayName => "Nhân viên kiểm kê";

    public string? RoleCode => "INVENTORY_STAFF";
}

public sealed class NoopUnitOfWork : IUnitOfWork
{
    public int SaveCalls { get; private set; }

    public int TransactionCalls { get; private set; }

    public Task<IUnitOfWorkTransaction> BeginTransactionAsync(
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default)
    {
        TransactionCalls++;
        return Task.FromResult<IUnitOfWorkTransaction>(new NoopTransaction());
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCalls++;
        return Task.CompletedTask;
    }

    public Task<TResult> ExecuteAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default)
        => operation(cancellationToken);

    private sealed class NoopTransaction : IUnitOfWorkTransaction
    {
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
