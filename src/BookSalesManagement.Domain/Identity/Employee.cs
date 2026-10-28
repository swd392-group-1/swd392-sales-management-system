namespace BookSalesManagement.Domain.Identity;

public sealed class Employee
{
    private Employee()
    {
        Id = string.Empty;
        RoleCode = string.Empty;
    }

    public string Id { get; private set; }

    public string? Name { get; private set; }

    public string RoleCode { get; private set; }
}
