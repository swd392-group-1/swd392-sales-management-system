using BookSalesManagement.Application.Abstractions;
using BookSalesManagement.Application.Dtos;
using BookSalesManagement.Domain.Inventory;
using Microsoft.EntityFrameworkCore;

namespace BookSalesManagement.Infrastructure.Persistence;

public sealed class EfInventoryAuditStore : IInventoryAuditStore
{
    private readonly BookSalesDbContext _db;

    public EfInventoryAuditStore(BookSalesDbContext db)
    {
        _db = db;
    }

    public Task<InventoryAudit?> FindAsync(string auditId, CancellationToken cancellationToken = default)
        => _db.InventoryAudits
            .Include(audit => audit.Lines)
            .SingleOrDefaultAsync(audit => audit.Id == auditId, cancellationToken);

    public Task AddAsync(InventoryAudit audit, CancellationToken cancellationToken = default)
    {
        _db.InventoryAudits.Add(audit);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<InventoryAuditSummary>> ListAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _db.InventoryAudits
            .AsNoTracking()
            .OrderByDescending(audit => audit.AuditDate)
            .ThenByDescending(audit => audit.Id)
            .Select(audit => new
            {
                audit.Id,
                audit.AuditDate,
                audit.Status,
                LineCount = audit.Lines.Count
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new InventoryAuditSummary(
                row.Id,
                row.AuditDate,
                row.Status ?? InventoryAudit.DraftStatus,
                row.LineCount))
            .ToArray();
    }
}
