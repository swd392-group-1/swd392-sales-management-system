
using BookSales.Domain.Interfaces;

namespace BookSales.Application.Services
{
    public class PricingService
    {
        private readonly IBookRepository _bookRepository;
        private readonly IBackgroundTaskQueue _taskQueue;

        public PricingService(
            IBookRepository bookRepository,
            IBackgroundTaskQueue taskQueue)
        {
            _bookRepository = bookRepository;
            _taskQueue = taskQueue;
        }

        public async Task<bool> UpdateCurrentPriceAsync(
            string bookId,
            decimal newPrice)
        {
            var book = await _bookRepository.GetByIdAsync(bookId);

            if (book == null)
                return false;

            book.ChangeCurrentPrice(newPrice);

            var isCommitted =
                await _bookRepository.SaveAndCommitAsync();

            if (isCommitted)
            {
                var priceChangedEvent = new Notifications.BookPriceChangedEvent
                {
                    BookId = book.BookId,
                    CurrentPrice = book.CurrentPrice
                };

                await _taskQueue.EnqueueAsync(priceChangedEvent);
            }

            return isCommitted;
        }
    }
}
