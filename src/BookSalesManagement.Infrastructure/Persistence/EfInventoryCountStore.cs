using BookSalesManagement.Application.Abstractions;
using BookSalesManagement.Application.Dtos;
using BookSalesManagement.Domain.Inventory;
using Microsoft.EntityFrameworkCore;

namespace BookSalesManagement.Infrastructure.Persistence;

public sealed class EfInventoryCountStore : IInventoryCountStore
{
    private readonly BookSalesDbContext _db;

    public EfInventoryCountStore(BookSalesDbContext db)
    {
        _db = db;
    }

    public Task<InventoryCount?> FindAsync(string countId, CancellationToken cancellationToken = default)
        => _db.InventoryCounts
            .Include(count => count.Observations)
            .Include(count => count.Selections)
            .SingleOrDefaultAsync(count => count.Id == countId, cancellationToken);

    public Task AddAsync(InventoryCount count, CancellationToken cancellationToken = default)
    {
        _db.InventoryCounts.Add(count);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<InventoryCountSummary>> ListAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _db.InventoryCounts
            .AsNoTracking()
            .OrderByDescending(count => count.CountedAtUtc)
            .Select(count => new
            {
                count.Id,
                count.CountedAtUtc,
                count.RecordedByEmployeeId,
                BookCount = count.Observations.Select(observation => observation.BookId).Distinct().Count(),
                ObservationCount = count.Observations.Count,
                IsFinalized = count.Selections.Any(),
                AdjustmentId = _db.CountAdjustments
                    .Where(link => link.CountId == count.Id)
                    .Select(link => link.AdjustmentId)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        var employeeIds = rows.Select(row => row.RecordedByEmployeeId).Distinct().ToArray();
        var employeeNames = await _db.Employees
            .AsNoTracking()
            .Where(employee => employeeIds.Contains(employee.Id))
            .ToDictionaryAsync(employee => employee.Id, employee => employee.Name ?? employee.Id, cancellationToken);

        return rows
            .Select(row => new InventoryCountSummary(
                row.Id,
                row.CountedAtUtc,
                employeeNames.TryGetValue(row.RecordedByEmployeeId, out var name) ? name : row.RecordedByEmployeeId,
                row.BookCount,
                row.ObservationCount,
                row.IsFinalized,
                row.AdjustmentId))
            .ToArray();
    }

    public async Task<string?> FindAdjustmentIdAsync(string countId, CancellationToken cancellationToken = default)
        => await _db.CountAdjustments
            .AsNoTracking()
            .Where(link => link.CountId == countId)
            .Select(link => link.AdjustmentId)
            .FirstOrDefaultAsync(cancellationToken);
}
