using BookSales.Application.Returns;

namespace BookSales.Web.Models;

public sealed class BookReturnHistoryViewModel
{
    public string? OrderId { get; set; }
    public IReadOnlyList<BookReturnSummary> Returns { get; set; } =
        Array.Empty<BookReturnSummary>();
}
