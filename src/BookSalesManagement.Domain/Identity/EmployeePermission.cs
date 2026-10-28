namespace BookSalesManagement.Domain.Identity;

public sealed class EmployeePermission
{
    private EmployeePermission()
    {
        EmployeeId = string.Empty;
        PermissionId = string.Empty;
    }

    public string EmployeeId { get; private set; }

    public string PermissionId { get; private set; }
}
