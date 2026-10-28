using BookSalesManagement.Application.Abstractions;
using BookSalesManagement.Application.Dtos;
using Microsoft.EntityFrameworkCore;

namespace BookSalesManagement.Infrastructure.Persistence;

public sealed class BookDirectory : IBookDirectory
{
    private readonly BookSalesDbContext _db;

    public BookDirectory(BookSalesDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<BookOption>> GetOptionsAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _db.Books
            .AsNoTracking()
            .OrderBy(book => book.Title)
            .Select(book => new
            {
                book.Id,
                book.Title,
                CurrentQuantity = _db.BookStocks
                    .Where(stock => stock.BookId == book.Id)
                    .Select(stock => (decimal?)stock.CurrentQuantity)
                    .FirstOrDefault() ?? 0
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new BookOption(row.Id, row.Title ?? row.Id, row.CurrentQuantity))
            .ToArray();
    }

    public async Task<IReadOnlyDictionary<string, string>> GetTitlesAsync(
        IEnumerable<string> bookIds,
        CancellationToken cancellationToken = default)
    {
        var ids = bookIds.Distinct().ToArray();
        var rows = await _db.Books
            .AsNoTracking()
            .Where(book => ids.Contains(book.Id))
            .Select(book => new { book.Id, book.Title })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(row => row.Id, row => row.Title ?? row.Id);
    }

    public async Task<bool> AllExistAsync(IEnumerable<string> bookIds, CancellationToken cancellationToken = default)
    {
        var ids = bookIds.Distinct().ToArray();
        if (ids.Length == 0)
        {
            return false;
        }

        var found = await _db.Books
            .AsNoTracking()
            .CountAsync(book => ids.Contains(book.Id), cancellationToken);

        return found == ids.Length;
    }
}

public sealed class EmployeeDirectory : IEmployeeDirectory
{
    private readonly BookSalesDbContext _db;

    public EmployeeDirectory(BookSalesDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyDictionary<string, string>> GetNamesAsync(
        IEnumerable<string> employeeIds,
        CancellationToken cancellationToken = default)
    {
        var ids = employeeIds.Distinct().ToArray();
        var rows = await _db.Employees
            .AsNoTracking()
            .Where(employee => ids.Contains(employee.Id))
            .Select(employee => new { employee.Id, employee.Name })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(row => row.Id, row => row.Name ?? row.Id);
    }
}

public sealed class AccountDirectory : IAccountDirectory
{
    private readonly BookSalesDbContext _db;

    public AccountDirectory(BookSalesDbContext db)
    {
        _db = db;
    }

    public async Task<DemoAccount?> FindAsync(string accountId, CancellationToken cancellationToken = default)
    {
        var rows = await Project(_db.Accounts.Where(account => account.Id == accountId))
            .Take(1)
            .ToListAsync(cancellationToken);

        return rows.FirstOrDefault();
    }

    public async Task<IReadOnlyList<DemoAccount>> ListAsync(CancellationToken cancellationToken = default)
    {
        var rows = await Project(_db.Accounts).ToListAsync(cancellationToken);
        return rows.OrderBy(row => row.Username, StringComparer.Ordinal).ToArray();
    }

    private IQueryable<DemoAccount> Project(IQueryable<BookSalesManagement.Domain.Identity.Account> accounts)
        => from account in accounts.AsNoTracking()
           join employee in _db.Employees.AsNoTracking() on account.EmployeeId equals employee.Id
           select new DemoAccount(
               account.Id,
               account.EmployeeId,
               account.Username,
               employee.Name ?? employee.Id,
               employee.RoleCode,
               account.Status ?? "DISABLED",
               account.Status == "ACTIVE");
}
