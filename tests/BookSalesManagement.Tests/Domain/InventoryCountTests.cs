using BookSalesManagement.Domain.Common;
using BookSalesManagement.Domain.Inventory;

namespace BookSalesManagement.Tests.Domain;

public sealed class InventoryCountTests
{
    private static readonly DateTime Now = new(2026, 10, 27, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Finalize_SecondTime_IsRejected()
    {
        var count = NewCount("CNT-1");
        var observation = count.AddObservation("B01", 12, Now);

        count.Finalize(
            new Dictionary<string, long> { ["B01"] = observation.Id },
            new Dictionary<string, decimal> { ["B01"] = 10 });

        var exception = Assert.Throws<DomainException>(() => count.Finalize(
            new Dictionary<string, long> { ["B01"] = observation.Id },
            new Dictionary<string, decimal> { ["B01"] = 10 }));

        Assert.Contains("đã được chốt", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddObservation_AfterFinalize_IsRejected()
    {
        var count = NewCount("CNT-2");
        var observation = count.AddObservation("B01", 12, Now);
        count.Finalize(
            new Dictionary<string, long> { ["B01"] = observation.Id },
            new Dictionary<string, decimal> { ["B01"] = 10 });

        Assert.Throws<DomainException>(() => count.AddObservation("B02", 3, Now));
    }

    [Fact]
    public void AddObservation_WithNegativeQuantity_IsRejected()
    {
        var count = NewCount("CNT-3");

        Assert.Throws<DomainException>(() => count.AddObservation("B01", -1, Now));
        Assert.Empty(count.Observations);
    }

    [Fact]
    public void Finalize_WithoutSelectionForEveryBook_IsRejected()
    {
        var count = NewCount("CNT-4");
        var first = count.AddObservation("B01", 12, Now);
        count.AddObservation("B02", 7, Now);

        var exception = Assert.Throws<DomainException>(() => count.Finalize(
            new Dictionary<string, long> { ["B01"] = first.Id },
            new Dictionary<string, decimal> { ["B01"] = 10, ["B02"] = 7 }));

        Assert.Contains("Chưa chọn kết quả đếm", exception.Message);
        Assert.False(count.IsFinalized);
    }

    [Fact]
    public void Finalize_WithoutObservations_IsRejected()
    {
        var count = NewCount("CNT-5");

        Assert.Throws<DomainException>(() => count.Finalize(
            new Dictionary<string, long>(),
            new Dictionary<string, decimal>()));
        Assert.False(count.IsFinalized);
    }

    private static InventoryCount NewCount(string id) => new(id, Now, "EMP-01");
}
