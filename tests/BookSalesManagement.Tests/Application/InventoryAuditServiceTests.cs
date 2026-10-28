using BookSalesManagement.Application.Dtos;
using BookSalesManagement.Application.Services;
using BookSalesManagement.Domain.Common;
using BookSalesManagement.Tests.Fakes;

namespace BookSalesManagement.Tests.Application;

public sealed class InventoryAuditServiceTests
{
    private static readonly DateTime Now = new(2026, 10, 27, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Approve_OnlyConfirmsResultsAndLeavesStockUntouched()
    {
        var harness = new Harness(("B01", 10), ("B02", 5));
        var auditId = await harness.Service.CreateAsync(new[]
        {
            new NewCountLine("B01", 12),
            new NewCountLine("B02", 4)
        });
        var audit = await harness.Audits.FindAsync(auditId);

        await harness.Service.ApproveAsync(auditId);

        Assert.True(audit!.IsApproved);
        Assert.Equal("APPROVED", audit.Status);
        Assert.Equal(10, harness.Stock.Quantities["B01"]);
        Assert.Equal(5, harness.Stock.Quantities["B02"]);
        Assert.Equal(0, harness.Stock.TrySetCalls);
        Assert.Single(harness.Audits.All);
    }

    [Fact]
    public async Task Approve_SecondTime_IsRejected()
    {
        var harness = new Harness(("B01", 10));
        var auditId = await harness.Service.CreateAsync(new[] { new NewCountLine("B01", 12) });
        await harness.Service.ApproveAsync(auditId);

        await Assert.ThrowsAsync<DomainException>(() => harness.Service.ApproveAsync(auditId));
    }

    [Fact]
    public async Task Create_WithNegativeQuantity_IsRejected()
    {
        var harness = new Harness(("B01", 10));

        await Assert.ThrowsAsync<DomainException>(() => harness.Service.CreateAsync(new[]
        {
            new NewCountLine("B01", -1)
        }));

        Assert.Empty(harness.Audits.All);
    }

    [Fact]
    public async Task Create_WithDuplicatedBook_IsRejected()
    {
        var harness = new Harness(("B01", 10));

        await Assert.ThrowsAsync<DomainException>(() => harness.Service.CreateAsync(new[]
        {
            new NewCountLine("B01", 12),
            new NewCountLine("B01", 13)
        }));

        Assert.Empty(harness.Audits.All);
    }

    private sealed class Harness
    {
        public Harness(params (string BookId, decimal Quantity)[] stock)
        {
            foreach (var (bookId, quantity) in stock)
            {
                Stock.Quantities[bookId] = quantity;
                Books.Existing.Add(bookId);
            }

            Service = new InventoryAuditService(
                Audits,
                Stock,
                Books,
                UnitOfWork,
                new TestCurrentUser(),
                new FixedClock(Now));
        }

        public InMemoryAuditStore Audits { get; } = new();

        public InMemoryStockStore Stock { get; } = new();

        public InMemoryBookDirectory Books { get; } = new();

        public NoopUnitOfWork UnitOfWork { get; } = new();

        public InventoryAuditService Service { get; }
    }
}
