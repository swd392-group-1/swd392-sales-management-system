namespace BookSales.Domain.Entities
{
    public class Book
    {
        public required string BookId { get; init; }
        public required string Title { get; init; }
        public decimal CurrentPrice { get; private set; }

        public void ChangeCurrentPrice(decimal newPrice)
        {
            if (newPrice < 0)
                throw new ArgumentException("Price cannot be negative.");

            CurrentPrice = newPrice;
        }
    }
}