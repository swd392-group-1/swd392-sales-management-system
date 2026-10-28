using System.Data;
using BookSalesManagement.Application.Abstractions;
using BookSalesManagement.Application.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace BookSalesManagement.Infrastructure.Persistence;

public sealed class EfUnitOfWork : IUnitOfWork
{
    private readonly BookSalesDbContext _db;

    public EfUnitOfWork(BookSalesDbContext db)
    {
        _db = db;
    }

    public async Task<IUnitOfWorkTransaction> BeginTransactionAsync(
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default)
    {
        var transaction = await _db.Database.BeginTransactionAsync(isolationLevel, cancellationToken);
        return new EfUnitOfWorkTransaction(transaction);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            throw new ConflictException(
                "Dữ liệu đã bị thay đổi bởi thao tác khác. Không ghi trùng chênh lệch hoặc kết quả kiểm kê.",
                exception);
        }
    }

    public Task<TResult> ExecuteAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        var strategy = _db.Database.CreateExecutionStrategy();
        return strategy.ExecuteAsync(() => operation(cancellationToken));
    }

    private static bool IsUniqueViolation(DbUpdateException exception)
        => exception.InnerException is SqlException sqlException
           && sqlException.Number is 2601 or 2627;

    private sealed class EfUnitOfWorkTransaction : IUnitOfWorkTransaction
    {
        private readonly IDbContextTransaction _transaction;

        public EfUnitOfWorkTransaction(IDbContextTransaction transaction)
        {
            _transaction = transaction;
        }

        public Task CommitAsync(CancellationToken cancellationToken = default)
            => _transaction.CommitAsync(cancellationToken);

        public ValueTask DisposeAsync() => _transaction.DisposeAsync();
    }
}
