namespace BookSales.Application.Returns;

public static class ReturnPeriod
{
    public static bool IsWithinWindow(
        DateOnly? orderDate,
        DateOnly currentUtcDate)
    {
        if (orderDate is not { } date)
        {
            return false;
        }

        var daysSinceOrder = currentUtcDate.DayNumber - date.DayNumber;
        return daysSinceOrder is >= 0 and <= 7;
    }
}
