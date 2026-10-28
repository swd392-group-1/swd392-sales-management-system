namespace BookSalesManagement.Domain.Inventory;

public sealed class CountAdjustment
{
    private CountAdjustment()
    {
        CountId = string.Empty;
        AdjustmentId = string.Empty;
    }

    public CountAdjustment(string countId, string adjustmentId)
    {
        CountId = countId;
        AdjustmentId = adjustmentId;
    }

    public string CountId { get; private set; }

    public string AdjustmentId { get; private set; }
}
