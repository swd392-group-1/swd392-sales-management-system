using BookSalesManagement.Domain.Common;
using BookSalesManagement.Domain.Inventory;

namespace BookSalesManagement.Tests.Domain;

public sealed class InventoryAuditTests
{
    [Fact]
    public void Approve_ChangesStatusOnly()
    {
        var audit = new InventoryAudit("AUD-1", new DateOnly(2026, 10, 27));
        audit.AddLine("B01", 12);

        audit.Approve();

        Assert.True(audit.IsApproved);
        Assert.Equal(InventoryAudit.ApprovedStatus, audit.Status);
        Assert.Single(audit.Lines);
    }

    [Fact]
    public void Approve_SecondTime_IsRejected()
    {
        var audit = new InventoryAudit("AUD-2", new DateOnly(2026, 10, 27));
        audit.AddLine("B01", 12);
        audit.Approve();

        Assert.Throws<DomainException>(() => audit.Approve());
    }

    [Fact]
    public void Approve_WithoutLines_IsRejected()
    {
        var audit = new InventoryAudit("AUD-3", new DateOnly(2026, 10, 27));

        Assert.Throws<DomainException>(() => audit.Approve());
        Assert.False(audit.IsApproved);
    }

    [Fact]
    public void AddLine_AfterApproval_IsRejected()
    {
        var audit = new InventoryAudit("AUD-4", new DateOnly(2026, 10, 27));
        audit.AddLine("B01", 12);
        audit.Approve();

        Assert.Throws<DomainException>(() => audit.AddLine("B02", 3));
    }

    [Fact]
    public void AddLine_WithNegativeQuantity_IsRejected()
    {
        var audit = new InventoryAudit("AUD-5", new DateOnly(2026, 10, 27));

        Assert.Throws<DomainException>(() => audit.AddLine("B01", -1));
        Assert.Empty(audit.Lines);
    }
}
