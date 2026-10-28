namespace BookSalesManagement.Domain.Identity;

public sealed class Account
{
    private Account()
    {
        Id = string.Empty;
        EmployeeId = string.Empty;
        Username = string.Empty;
    }

    public string Id { get; private set; }

    public string EmployeeId { get; private set; }

    public string Username { get; private set; }

    public string? Status { get; private set; }

    public bool IsActive => string.Equals(Status, "ACTIVE", StringComparison.OrdinalIgnoreCase);
}
