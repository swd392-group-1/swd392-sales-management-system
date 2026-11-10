using BookSales.Domain.Entities;
using BookSales.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BookSales.Infrastructure.Persistence
{
    public class BookRepository : IBookRepository
    {
        private readonly BookSalesDbContext _context;

        public BookRepository(BookSalesDbContext context)
        {
            _context = context;
        }

        public async Task<Book?> GetByIdAsync(string bookId)
        {
            return await _context.Books
                .SingleOrDefaultAsync(b => b.BookId == bookId);
        }

        public async Task SaveAndCommitAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
