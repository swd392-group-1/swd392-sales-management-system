
namespace BookSales.Application.Access;

public sealed class StaffPermissionService(
    IStaffPermissionChangePolicy policy,
    IStaffPermissionStore store)
{
    public async Task<bool> ChangeAsync(
        StaffPermissionChange change,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(change);

        if (string.IsNullOrWhiteSpace(change.ActorAccountId) ||
            string.IsNullOrWhiteSpace(change.TargetEmployeeId) ||
            string.IsNullOrWhiteSpace(change.PermissionId))
        {
            return false;
        }

        var allowed = await policy.IsAllowedAsync(
            change,
            cancellationToken);

        if (!allowed)
        {
            return false;
        }

        await store.ApplyAsync(
            change.TargetEmployeeId,
            change.PermissionId,
            change.Grant,
            cancellationToken);

        return true;
    }
}
