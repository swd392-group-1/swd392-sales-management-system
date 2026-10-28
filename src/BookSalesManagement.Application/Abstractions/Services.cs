using System.Data;
using BookSalesManagement.Application.Dtos;

namespace BookSalesManagement.Application.Abstractions;

public interface IUnitOfWork
{
    Task<IUnitOfWorkTransaction> BeginTransactionAsync(
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<TResult> ExecuteAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default);
}

public interface IUnitOfWorkTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
}

public interface IBookDirectory
{
    Task<IReadOnlyList<BookOption>> GetOptionsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<string, string>> GetTitlesAsync(
        IEnumerable<string> bookIds,
        CancellationToken cancellationToken = default);

    Task<bool> AllExistAsync(IEnumerable<string> bookIds, CancellationToken cancellationToken = default);
}

public interface IEmployeeDirectory
{
    Task<IReadOnlyDictionary<string, string>> GetNamesAsync(
        IEnumerable<string> employeeIds,
        CancellationToken cancellationToken = default);
}

public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    string? AccountId { get; }

    string? EmployeeId { get; }

    string DisplayName { get; }

    string? RoleCode { get; }
}

public interface IPermissionChecker
{
    Task<bool> HasPermissionAsync(
        string employeeId,
        string permissionId,
        CancellationToken cancellationToken = default);
}

public interface IAccountDirectory
{
    Task<DemoAccount?> FindAsync(string accountId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DemoAccount>> ListAsync(CancellationToken cancellationToken = default);
}
