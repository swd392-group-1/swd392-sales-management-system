using BookSales.Application.Returns;

namespace BookSales.Tests;

public sealed class ReturnPeriodTests
{
    [Theory]
    [InlineData(0, true)]
    [InlineData(7, true)]
    [InlineData(8, false)]
    [InlineData(-1, false)]
    public void IsWithinWindow_UsesInclusiveUtcCalendarDayWindow(
        int daysAfterOrder,
        bool expected)
    {
        var orderDate = new DateOnly(2026, 10, 27);
        var currentDate = orderDate.AddDays(daysAfterOrder);

        Assert.Equal(
            expected,
            ReturnPeriod.IsWithinWindow(orderDate, currentDate));
    }

    [Fact]
    public void IsWithinWindow_RejectsMissingOrderDate()
    {
        Assert.False(ReturnPeriod.IsWithinWindow(
            null,
            new DateOnly(2026, 10, 27)));
    }
}
