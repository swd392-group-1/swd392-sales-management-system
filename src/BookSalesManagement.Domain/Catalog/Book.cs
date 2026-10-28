namespace BookSalesManagement.Domain.Catalog;

public sealed class Book
{
    private Book()
    {
        Id = string.Empty;
    }

    public string Id { get; private set; }

    public string? Title { get; private set; }

    public decimal? CurrentPrice { get; private set; }
}
