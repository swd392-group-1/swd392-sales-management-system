namespace BookSales.Application.Notifications
{
    public class BookPriceChangedEvent
    {
        public required string BookId { get; init; }
        public decimal CurrentPrice { get; init; }
    }
}
