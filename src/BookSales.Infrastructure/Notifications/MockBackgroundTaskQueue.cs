using BookSales.Domain.Interfaces;

namespace BookSales.Infrastructure.Notifications
{
    public class MockBackgroundTaskQueue : IBackgroundTaskQueue
    {
        public ValueTask EnqueueAsync(object workItem)
        {
            return ValueTask.CompletedTask;
        }
    }
}