using BookSalesManagement.Application.Common;
using BookSalesManagement.Application.Dtos;
using BookSalesManagement.Application.Services;
using BookSalesManagement.Domain.Common;
using BookSalesManagement.Domain.Inventory;
using BookSalesManagement.Tests.Fakes;

namespace BookSalesManagement.Tests.Application;

public sealed class InventoryCountServiceTests
{
    private static readonly DateTime Now = new(2026, 10, 27, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Finalize_PostsOneAdjustmentWithOnlyDifferingLines()
    {
        var harness = new Harness(("B01", 10), ("B02", 5), ("B03", 7));
        var countId = await harness.Service.CreateAsync(new[]
        {
            new NewCountLine("B01", 12),
            new NewCountLine("B02", 5),
            new NewCountLine("B03", 6)
        });

        var count = await harness.Counts.FindAsync(countId);
        var result = await harness.Service.FinalizeAsync(countId, SelectionsOf(count!));

        var adjustment = Assert.Single(harness.Adjustments.All);
        Assert.Equal(adjustment.Id, result.AdjustmentId);
        Assert.Equal(2, adjustment.Books.Count);
        Assert.DoesNotContain(adjustment.Books, book => book.BookId == "B02");
        Assert.Equal(2, adjustment.Books.Single(book => book.BookId == "B01").QuantityChange);
        Assert.Equal(-1, adjustment.Books.Single(book => book.BookId == "B03").QuantityChange);

        Assert.Equal(2, harness.Movements.All.Count);
        Assert.Equal(3, count!.Selections.Count);
        Assert.Equal(12, harness.Stock.Quantities["B01"]);
        Assert.Equal(5, harness.Stock.Quantities["B02"]);
        Assert.Equal(6, harness.Stock.Quantities["B03"]);
        Assert.Equal(1, harness.UnitOfWork.TransactionCalls);
    }

    [Fact]
    public async Task Finalize_WhenEveryLineMatches_DoesNotCreateAdjustment()
    {
        var harness = new Harness(("B01", 10), ("B02", 5));
        var countId = await harness.Service.CreateAsync(new[]
        {
            new NewCountLine("B01", 10),
            new NewCountLine("B02", 5)
        });

        var count = await harness.Counts.FindAsync(countId);
        var result = await harness.Service.FinalizeAsync(countId, SelectionsOf(count!));

        Assert.Null(result.AdjustmentId);
        Assert.Empty(harness.Adjustments.All);
        Assert.Empty(harness.Movements.All);
        Assert.True(count!.IsFinalized);
        Assert.Equal(10, harness.Stock.Quantities["B01"]);
        Assert.Equal(5, harness.Stock.Quantities["B02"]);
    }

    [Fact]
    public async Task Finalize_SecondTime_IsRejectedWithoutPostingAgain()
    {
        var harness = new Harness(("B01", 10), ("B02", 5));
        var countId = await harness.Service.CreateAsync(new[]
        {
            new NewCountLine("B01", 12),
            new NewCountLine("B02", 4)
        });
        var count = await harness.Counts.FindAsync(countId);
        var selections = SelectionsOf(count!);

        await harness.Service.FinalizeAsync(countId, selections);
        var exception = await Assert.ThrowsAsync<DomainException>(
            () => harness.Service.FinalizeAsync(countId, selections));

        Assert.Contains("đã được chốt", exception.Message);
        Assert.Single(harness.Adjustments.All);
        Assert.Equal(2, harness.Movements.All.Count);
    }

    [Fact]
    public async Task Finalize_WhenASelectionIsMissing_IsRejected()
    {
        var harness = new Harness(("B01", 10), ("B02", 5));
        var countId = await harness.Service.CreateAsync(new[]
        {
            new NewCountLine("B01", 12),
            new NewCountLine("B02", 7)
        });
        var count = await harness.Counts.FindAsync(countId);
        var firstBookObservation = count!.Observations.First(observation => observation.BookId == "B01");

        await Assert.ThrowsAsync<DomainException>(() => harness.Service.FinalizeAsync(
            countId,
            new Dictionary<string, long> { ["B01"] = firstBookObservation.Id }));

        Assert.Empty(harness.Adjustments.All);
        Assert.Empty(harness.Movements.All);
    }

    [Fact]
    public async Task Finalize_WhenStockChangedConcurrently_ThrowsConflictAndPostsNothing()
    {
        var harness = new Harness(("B01", 10), ("B02", 5));
        var countId = await harness.Service.CreateAsync(new[]
        {
            new NewCountLine("B01", 12),
            new NewCountLine("B02", 7)
        });
        var count = await harness.Counts.FindAsync(countId);
        harness.Stock.BeforeTrySet = () => harness.Stock.Quantities["B01"] = 99;

        await Assert.ThrowsAsync<ConflictException>(
            () => harness.Service.FinalizeAsync(countId, SelectionsOf(count!)));

        Assert.Empty(harness.Adjustments.All);
        Assert.Empty(harness.Movements.All);
        Assert.Equal(5, harness.Stock.Quantities["B02"]);
    }

    [Fact]
    public async Task Create_WithNegativeQuantity_IsRejected()
    {
        var harness = new Harness(("B01", 10));

        await Assert.ThrowsAsync<DomainException>(() => harness.Service.CreateAsync(new[]
        {
            new NewCountLine("B01", -2)
        }));

        Assert.Empty(harness.Counts.All);
    }

    [Fact]
    public async Task Create_WithUnknownBook_IsRejected()
    {
        var harness = new Harness(("B01", 10));

        await Assert.ThrowsAsync<DomainException>(() => harness.Service.CreateAsync(new[]
        {
            new NewCountLine("B99", 3)
        }));

        Assert.Empty(harness.Counts.All);
    }

    private static Dictionary<string, long> SelectionsOf(InventoryCount count)
        => count.Observations
            .GroupBy(observation => observation.BookId)
            .ToDictionary(
                group => group.Key,
                group => group.OrderBy(observation => observation.ObservedAtUtc).First().Id);

    private sealed class Harness
    {
        public Harness(params (string BookId, decimal Quantity)[] stock)
        {
            foreach (var (bookId, quantity) in stock)
            {
                Stock.Quantities[bookId] = quantity;
                Books.Existing.Add(bookId);
            }

            Service = new InventoryCountService(
                Counts,
                Stock,
                Adjustments,
                Movements,
                Books,
                new InMemoryEmployeeDirectory(),
                UnitOfWork,
                new TestCurrentUser(),
                new FixedClock(Now));
        }

        public InMemoryCountStore Counts { get; } = new();

        public InMemoryStockStore Stock { get; } = new();

        public InMemoryAdjustmentStore Adjustments { get; } = new();

        public InMemoryMovementStore Movements { get; } = new();

        public InMemoryBookDirectory Books { get; } = new();

        public NoopUnitOfWork UnitOfWork { get; } = new();

        public InventoryCountService Service { get; }
    }
}
