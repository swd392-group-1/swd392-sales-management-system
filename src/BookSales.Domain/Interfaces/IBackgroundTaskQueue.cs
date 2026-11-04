namespace BookSales.Domain.Interfaces
{
    public interface IBackgroundTaskQueue
    {
        ValueTask EnqueueAsync(object workItem);
    }
}