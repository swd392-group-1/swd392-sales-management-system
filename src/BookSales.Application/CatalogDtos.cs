public class UpdatePriceRequest
{
    public decimal NewPrice { get; set; }
}

public class BookPriceChangedEvent
{
    public int BookId { get; set; }
    public decimal CurrentPrice { get; set; }
}