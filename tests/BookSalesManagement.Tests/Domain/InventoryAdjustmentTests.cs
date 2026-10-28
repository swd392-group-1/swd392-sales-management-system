using BookSalesManagement.Domain.Inventory;

namespace BookSalesManagement.Tests.Domain;

public sealed class InventoryAdjustmentTests
{
    private static readonly DateTime Now = new(2026, 10, 27, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void CreateForCount_UsesSingleAdjustmentWithOnlyDifferingLines()
    {
        var lines = new[]
        {
            new FinalizedCountLine("B01", 1, 10, 12),
            new FinalizedCountLine("B02", 2, 5, 5),
            new FinalizedCountLine("B03", 3, 7, 6)
        };

        var adjustment = InventoryAdjustment.CreateForCount("CNT-1", "ADJ-1", Now, lines);

        Assert.NotNull(adjustment);
        Assert.Equal("ADJ-1", adjustment!.Id);
        Assert.Equal("Chênh lệch kiểm kê CNT-1", adjustment.Reason);
        Assert.Equal(2, adjustment.Books.Count);
        Assert.DoesNotContain(adjustment.Books, book => book.BookId == "B02");
        Assert.Equal(2, adjustment.Books.Single(book => book.BookId == "B01").QuantityChange);
        Assert.Equal(-1, adjustment.Books.Single(book => book.BookId == "B03").QuantityChange);
    }

    [Fact]
    public void CreateForCount_WhenNothingDiffers_ReturnsNull()
    {
        var lines = new[]
        {
            new FinalizedCountLine("B01", 1, 10, 10),
            new FinalizedCountLine("B02", 2, 5, 5)
        };

        var adjustment = InventoryAdjustment.CreateForCount("CNT-2", "ADJ-2", Now, lines);

        Assert.Null(adjustment);
    }

    [Fact]
    public void CreateForAdjustment_PostingKeyContainsEachPrefixOnce()
    {
        var lines = new[] { new FinalizedCountLine("B01", 1, 10, 12) };
        var adjustment = InventoryAdjustment.CreateForCount("CNT-3", "ADJ-3", Now, lines);
        var book = Assert.Single(adjustment!.Books);

        var movement = InventoryMovement.CreateForAdjustment(adjustment.Id, book, Now);

        Assert.Equal("ADJ-3-B01", movement.PostingKey);
        Assert.DoesNotContain("ADJ-ADJ", movement.PostingKey);
        Assert.Equal(2, movement.QuantityDelta);
    }
}
