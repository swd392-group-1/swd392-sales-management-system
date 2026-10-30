using BookSales.Domain.Entities;
using BookSales.Domain.Interfaces;

namespace BookSales.Infrastructure.Persistence
{
    public class BookRepository : IBookRepository
    {
        public async Task<Book?> GetByIdAsync(string bookId)
        {
            if (bookId != "DEMO-20261027-B01") return null;

            var book = new Book
            {
                BookId = bookId,
                Title = "Thiết kế hướng đối tượng [DEMO]"
            };
            book.ChangeCurrentPrice(130000);

            return await Task.FromResult(book);
        }

        public async Task<bool> SaveAndCommitAsync()
        {
            return await Task.FromResult(true);
        }
    }
}