
namespace BookSales.Application.Access;

public sealed record StaffPermissionChange(
    string ActorAccountId,
    string TargetEmployeeId,
    string PermissionId,
    bool Grant);

public interface IStaffPermissionChangePolicy
{
    Task<bool> IsAllowedAsync(
        StaffPermissionChange change,
        CancellationToken cancellationToken = default);
}

public interface IStaffPermissionStore
{
    Task ApplyAsync(
        string employeeId,
        string permissionId,
        bool grant,
        CancellationToken cancellationToken = default);
}
