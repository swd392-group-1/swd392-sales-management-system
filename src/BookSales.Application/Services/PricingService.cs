using BookSales.Application.Notifications;
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
            if (string.IsNullOrWhiteSpace(bookId) || bookId.Length > 200) throw new ArgumentException("Invalid Book ID.");
            //Lấy thông tin sách từ DB
            var book = await _bookRepository.GetByIdAsync(bookId);
            if (book == null)
                return false;

            //Xác thực và đổi giá 
            book.ChangeCurrentPrice(newPrice);

            //Lưu xuống Database và Commit
            await _bookRepository.SaveAndCommitAsync();

            //Đưa sự kiện đổi giá vào hàng đợi
            var priceChangedEvent = new BookPriceChangedEvent
            {
                BookId = book.BookId,
                CurrentPrice = newPrice
            };
            await _taskQueue.EnqueueAsync(priceChangedEvent);
            return true;
        }
    }
}