using BookSalesManagement.Domain.Common;

namespace BookSalesManagement.Domain.Inventory;

public sealed class AdjustmentBook
{
    private AdjustmentBook()
    {
        AdjustmentId = string.Empty;
        BookId = string.Empty;
    }

    internal AdjustmentBook(string adjustmentId, string bookId, decimal quantityChange)
    {
        if (quantityChange == 0)
        {
            throw new DomainException("Dòng điều chỉnh phải có chênh lệch khác 0.");
        }

        AdjustmentId = adjustmentId;
        BookId = bookId;
        QuantityChange = quantityChange;
    }

    public string AdjustmentId { get; private set; }

    public string BookId { get; private set; }

    public decimal QuantityChange { get; private set; }
}
