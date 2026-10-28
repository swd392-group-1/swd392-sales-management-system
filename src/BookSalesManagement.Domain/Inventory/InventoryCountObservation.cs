using BookSalesManagement.Domain.Common;

namespace BookSalesManagement.Domain.Inventory;

public sealed class InventoryCountObservation
{
    private InventoryCountObservation()
    {
        CountId = string.Empty;
        BookId = string.Empty;
    }

    internal InventoryCountObservation(long id, string countId, string bookId, decimal countedQuantity, DateTime observedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(bookId))
        {
            throw new DomainException("Book của dòng kiểm kê không hợp lệ.");
        }

        if (countedQuantity < 0)
        {
            throw new DomainException("Số lượng đếm không được âm.");
        }

        Id = id;
        CountId = countId;
        BookId = bookId;
        CountedQuantity = countedQuantity;
        ObservedAtUtc = observedAtUtc;
    }

    public long Id { get; private set; }

    public string CountId { get; private set; }

    public string BookId { get; private set; }

    public decimal CountedQuantity { get; private set; }

    public DateTime ObservedAtUtc { get; private set; }
}
