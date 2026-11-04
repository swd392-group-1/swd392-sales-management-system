using BookSales.Domain.Entities;

namespace BookSales.Domain.Interfaces
{
    public interface IBookRepository
    {
        Task<Book?> GetByIdAsync(string bookId);
        Task SaveAndCommitAsync();
    }
}
