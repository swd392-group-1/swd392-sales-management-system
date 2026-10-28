namespace BookSalesManagement.Domain.Identity;

public sealed class Permission
{
    private Permission()
    {
        Id = string.Empty;
    }

    public string Id { get; private set; }

    public string? Name { get; private set; }
}
