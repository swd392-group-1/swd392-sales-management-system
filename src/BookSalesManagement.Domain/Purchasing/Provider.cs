namespace BookSalesManagement.Domain.Purchasing;

public sealed class Provider
{
    private Provider()
    {
        Id = string.Empty;
    }

    public string Id { get; private set; }

    public string? Name { get; private set; }
}
